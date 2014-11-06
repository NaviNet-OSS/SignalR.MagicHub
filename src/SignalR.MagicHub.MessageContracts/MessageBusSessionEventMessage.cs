namespace SignalR.MagicHub.MessageContracts
{
    /// <summary>
    /// Base class for all messages that will be sent to the message bus for network intelligence that relate to a user's session
    /// </summary>
    public abstract class MessageBusSessionEventMessage : BaseSerializationModel
    {
        public object User { get; set; }
    }
}