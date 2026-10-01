using System;
using System.Collections.Generic;
using Mirror;
using VContainer.Unity;
using UnityEngine;

public class CustomNetworkMessageService : INetworkMessageService, IInitializable, IDisposable
{
    private readonly Dictionary<string, HashSet<NetworkConnectionToClient>> _serverSubscriptions = new();
    private readonly Dictionary<Type, Delegate> _clientHandlers = new();

    public event Action<NetworkConnectionToClient, string> OnClientSubscribed;

    public void Initialize()
    {
        NetworkServer.RegisterHandler<SubscribeMessage>(OnServerReceiveSubscribe);
        NetworkServer.RegisterHandler<UnsubscribeMessage>(OnServerReceiveUnsubscribe);
        
        // Очистка мертвых подключений
        NetworkServer.OnDisconnectedEvent += OnServerDisconnect;
    }

    public void Dispose()
    {
        NetworkServer.UnregisterHandler<SubscribeMessage>();
        NetworkServer.UnregisterHandler<UnsubscribeMessage>();
        NetworkServer.OnDisconnectedEvent -= OnServerDisconnect;
    }

    public void Subscribe<T>(Action<T> handler) where T : struct, NetworkMessage
    {
        var type = typeof(T);
        
        if (!_clientHandlers.ContainsKey(type))
        {
            _clientHandlers[type] = handler;
            
            // Регистрируем обработчик в Mirror (чтобы избежать отключения при получении)
            NetworkClient.RegisterHandler<T>(OnClientReceiveMessage<T>, false);

            Action sendSub = () => NetworkClient.Send(new SubscribeMessage { MessageTypeName = type.FullName });

            if (NetworkClient.active)
            {
                sendSub.Invoke();
            }
            else
            {
                // Если подписались до подключения, отправляем после успешного коннекта
                NetworkClient.OnConnectedEvent += sendSub; 
            }
        }
        else
        {
            _clientHandlers[type] = Delegate.Combine(_clientHandlers[type], handler);
        }
    }

    public void Unsubscribe<T>(Action<T> handler) where T : struct, NetworkMessage
    {
        var type = typeof(T);
        if (_clientHandlers.ContainsKey(type))
        {
            _clientHandlers[type] = Delegate.Remove(_clientHandlers[type], handler);
            
            if (_clientHandlers[type] == null)
            {
                _clientHandlers.Remove(type);
                NetworkClient.UnregisterHandler<T>();
                
                if (NetworkClient.active)
                {
                    NetworkClient.Send(new UnsubscribeMessage { MessageTypeName = type.FullName });
                }
            }
        }
    }

    public void SendToSubscribers<T>(T message) where T : struct, NetworkMessage
    {
        var typeName = typeof(T).FullName;
        if (_serverSubscriptions.TryGetValue(typeName, out var connections))
        {
            foreach (var conn in connections)
            {
                conn.Send(message);
            }
        }
    }

    private void OnClientReceiveMessage<T>(T message) where T : struct, NetworkMessage
    {
        if (_clientHandlers.TryGetValue(typeof(T), out var del))
        {
            (del as Action<T>)?.Invoke(message);
        }
    }

    private void OnServerReceiveSubscribe(NetworkConnectionToClient conn, SubscribeMessage msg)
    {
        if (!_serverSubscriptions.TryGetValue(msg.MessageTypeName, out var subs))
        {
            subs = new HashSet<NetworkConnectionToClient>();
            _serverSubscriptions[msg.MessageTypeName] = subs;
        }
        subs.Add(conn);
        
        OnClientSubscribed?.Invoke(conn, msg.MessageTypeName);
    }

    private void OnServerReceiveUnsubscribe(NetworkConnectionToClient conn, UnsubscribeMessage msg)
    {
        if (_serverSubscriptions.TryGetValue(msg.MessageTypeName, out var subs))
        {
            subs.Remove(conn);
        }
    }

    private void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        foreach (var subs in _serverSubscriptions.Values)
        {
            subs.Remove(conn);
        }
    }
}