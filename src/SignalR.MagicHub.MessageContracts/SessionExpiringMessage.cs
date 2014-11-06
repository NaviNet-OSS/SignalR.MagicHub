using System;

namespace SignalR.MagicHub.MessageContracts
{
    public class SessionExpiringMessage : MessageBusSessionEventMessage
    {
        public DateTime ExpiresAt { get; set; }
    }
}