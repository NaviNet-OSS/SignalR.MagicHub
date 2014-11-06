namespace SignalR.MagicHub.MessageContracts
{
    public class SessionKeptAliveMessage : MessageBusSessionEventMessage
    {
        public string Status { get; set; }
    }
}