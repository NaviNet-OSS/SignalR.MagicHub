using System;

namespace SignalR.MagicHub.SessionValidator
{
    /// <summary>
    /// Interface for providing and mocking date and time
    /// </summary>
    public interface ISystemTime
    {
        /// <summary>
        /// Gets the current date and time.
        /// </summary>
        /// <value>
        /// The current date and time.
        /// </value>
        DateTime Now { get; }
    }
}
