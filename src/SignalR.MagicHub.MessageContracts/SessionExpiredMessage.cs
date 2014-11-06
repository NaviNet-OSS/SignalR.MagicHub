namespace SignalR.MagicHub.MessageContracts
{
    public sealed class SessionExpiredMessage : MessageBusSessionEventMessage
    {
        public string Status { get; set; }
    }
}