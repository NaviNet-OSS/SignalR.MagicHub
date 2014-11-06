using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.MagicHub.SessionValidator
{
    /// <summary>
    /// Configuration settings for the session validator service
    /// </summary>
    public sealed class SessionValidatorConfiguration
    {
        /// <summary>
        /// Gets or sets the number of seconds in advance to warn a user of an immenent ssession expiration.
        /// </summary>

        public int WarnUserSessionExpirationSeconds { get; set; }

        /// <summary>
        /// Gets or sets the frequency at which the session validator should check sessions.
        /// </summary>
        /// <value>
        /// The session validator run frequency in seconds.
        /// </value>
        public int SessionValidatorRunFrequencySeconds { get; set; }
    }
}
