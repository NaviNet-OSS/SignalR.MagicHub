using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalR.MagicHub.SessionValidator
{
    /// <summary>
    /// List of reasons for session ending
    /// </summary>
    public enum SessionEndingReason
    {
        /// <summary>
        /// Occurs when an administrator logs a user out.
        /// </summary>
        ADMINISTRATIVE_LOGOUT,
        /// <summary>
        /// Occurs when a user is logged out due to a concurrent login
        /// </summary>
        MULTIPLE_LOGIN,
        /// <summary>
        /// Occurs when a user's session expires
        /// </summary>
        EXPIRED
    }
}
