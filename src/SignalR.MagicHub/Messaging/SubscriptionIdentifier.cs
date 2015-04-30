using System;
using System.Text;
using System.Text.RegularExpressions;

namespace SignalR.MagicHub.Messaging
{
    /// <summary>
    /// Represents a subscription to a MagicHub topic which can optionally have a filter to further 
    /// narrow the scope of messages received.
    /// </summary>
    public sealed class SubscriptionIdentifier
    {
        /// <summary>
        /// Gets the topic.
        /// </summary>
        /// <value>
        /// The topic.
        /// </value>
        public string Topic { get; private set; }
        /// <summary>
        /// Gets the filter.
        /// </summary>
        /// <value>
        /// The filter.
        /// </value>
        public string Filter { get; private set; }
        /// <summary>
        /// Gets the selector, which represents both topic and filter.
        /// </summary>
        /// <value>
        /// The selector.
        /// </value>
        public string Selector { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionIdentifier"/> class.
        /// </summary>
        /// <param name="topic">The topic.</param>
        /// <param name="filter">The filter.</param>
        /// <exception cref="System.ArgumentNullException">Topic must be specified.</exception>
        public SubscriptionIdentifier(string topic, string filter)
        {
            Topic = topic;
            Filter = filter;

            if (topic == null)
            {
                throw new ArgumentNullException("Topic must be specified.");
            }

            var sb = new StringBuilder();
            sb.AppendFormat("Topic = '{0}'", topic);
            if (!string.IsNullOrWhiteSpace(filter))
            {
                sb.AppendFormat(" and {0}", filter);
            }

            Selector = sb.ToString();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionIdentifier"/> class.
        /// </summary>
        /// <param name="selector">The selector.</param>
        public SubscriptionIdentifier(string selector)
        {
            Selector = selector;

            var regex = new Regex(@"Topic = '(?<topic>[^\s]+)'( and)?( ?)(?<filter>.*)");
            var match = regex.Match(selector);

            Topic = match.Groups["topic"].Value;
            Filter = match.Groups["filter"].Value;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            SubscriptionIdentifier other = obj as SubscriptionIdentifier;
            return other != null && other.Selector.Equals(Selector);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return Selector.GetHashCode();
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Selector;
        }

        
    }
}