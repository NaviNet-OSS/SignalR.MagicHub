using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.MagicHub.SessionValidator
{
    public sealed class SessionStateEventArgs : EventArgs
    {
        public SessionStateEventArgs(ISessionState sessionState)
        {
            SessionState = sessionState;
            OperationSuccess = true;
        }

        /// <summary>
        /// Gets the state of the session associated with this event.
        /// </summary>
        /// <value>
        /// The state of the session.
        /// </value>
        public ISessionState SessionState { get; private set; }

        /// <summary>
        /// Gets or sets the value which indicates when a session expires for expiration notifcation messages only.
        /// </summary>
        /// <value>
        /// If an expiring event, the time at which the session is scheduled to expire, otherwise MinValue
        /// </value>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a keep-alive event is a success.
        /// </summary>
        /// <value>
        ///   <c>true</c> if keep-alive succeeded, or not a keep-alive event; otherwise, <c>false</c>.
        /// </value>
        public bool OperationSuccess { get; set; }

        /// <summary>
        /// Gets or sets the event details.
        /// </summary>
        /// <value>
        /// For expired event, the expiration reason, otherwise undefined.
        /// </value>
        public string EventDetails { get; set; }
    }
}
