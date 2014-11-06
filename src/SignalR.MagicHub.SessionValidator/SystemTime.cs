using System;

namespace SignalR.MagicHub.SessionValidator
{
    /// <summary>
    /// System time provider that uses DateTime.Now
    /// </summary>
    public class SystemTime : ISystemTime
    {
        private static readonly Lazy<ISystemTime> _instance = new Lazy<ISystemTime>(() => new SystemTime());
        private SystemTime()
        {
            
        }

        /// <summary>
        /// Gets the current instance.
        /// </summary>
        /// <value>
        /// The current.
        /// </value>
        public static ISystemTime Current
        {
            get { return _instance.Value; }
        }

        /// <summary>
        /// Gets the current time and date.
        /// </summary>
        /// <value>
        /// DateTime.Now.
        /// </value>
        public DateTime Now
        {
            get { return DateTime.Now; }
        }
    }
}
