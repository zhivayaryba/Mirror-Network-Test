using Mirror;

public struct SubscribeMessage : NetworkMessage
{
    public string MessageTypeName;
}

public struct UnsubscribeMessage : NetworkMessage
{
    public string MessageTypeName;
}

public struct HelloMessage : NetworkMessage
{
    public string Text;
}