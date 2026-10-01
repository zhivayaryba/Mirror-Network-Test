using UnityEngine;
using Mirror;
using VContainer;

public class ScenarioController : MonoBehaviour
{
    private INetworkMessageService _messagingService;
    private NetworkManager _networkManager;

    [Inject]
    public void Construct(INetworkMessageService messagingService)
    {
        _messagingService = messagingService;
        _messagingService.OnClientSubscribed += HandleClientSubscribed;
    }

    private void Awake()
    {
        _networkManager = FindObjectOfType<NetworkManager>();
    }

    // Шаг 1: Запускается сервер (или хост)
    [ContextMenu("1. Start Host")]
    public void Step1_StartHost()
    {
        Debug.Log("1. Запуск хоста...");
        _networkManager.StartHost();
    }

    // Шаг 2: Подключается клиент (для теста можно запустить второй инстанс)
    [ContextMenu("2. Start Client")]
    public void Step2_ConnectClient()
    {
        Debug.Log("2. Подключение клиента...");
        _networkManager.StartClient();
    }

    // Шаг 3: Клиент подписывается на получение сообщения
    [ContextMenu("3. Subscribe Client")]
    public void Step3_SubscribeClient()
    {
        Debug.Log("3. Клиент подписывается на HelloMessage...");
        _messagingService.Subscribe<HelloMessage>(OnHelloMessageReceived);
    }

    // Шаг 4: Сервер получает информацию о подписке клиента
    private void HandleClientSubscribed(NetworkConnectionToClient conn, string messageType)
    {
        if (messageType == typeof(HelloMessage).FullName)
        {
            Debug.Log($"4. Сервер получил подписку от клиента {conn.connectionId}.");
            Step5_ServerSendMessage();
        }
    }

    // Шаг 5: Сервер отправляет клиенту сообщение
    private void Step5_ServerSendMessage()
    {
        Debug.Log("5. Сервер отправляет HelloMessage подписанным клиентам...");
        _messagingService.SendToSubscribers(new HelloMessage { Text = "Hello Client!" });
    }

    // Шаг 6: Клиент принимает сообщение и выводит текст в консоль
    private void OnHelloMessageReceived(HelloMessage msg)
    {
        Debug.Log($"6. Клиент получил сообщение: {msg.Text}");
    }
}