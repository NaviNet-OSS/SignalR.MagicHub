using System.Collections.Generic;

namespace SignalR.MagicHub.Messaging
{
    /// <summary>
    /// Represents a message going through magichub
    /// </summary>
    public interface IMagicHubMessage
    {
        /// <summary>
        /// Gets the message body.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        string Message { get; }
        /// <summary>
        /// Gets the message properties.
        /// </summary>
        IReadOnlyDictionary<string, object> Context { get; }
    }
}