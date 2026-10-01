using System;
using Mirror;

public interface INetworkMessageService
{
    void Subscribe<T>(Action<T> handler) where T : struct, NetworkMessage;
    void Unsubscribe<T>(Action<T> handler) where T : struct, NetworkMessage;
    void SendToSubscribers<T>(T message) where T : struct, NetworkMessage;
    
    event Action<NetworkConnectionToClient, string> OnClientSubscribed;
}