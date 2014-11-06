using System;
using System.Text;
using System.Text.RegularExpressions;

namespace SignalR.MagicHub.Messaging
{
    public sealed class SubscriptionIdentifier
    {
        public string Topic { get; private set; }
        public string Filter { get; private set; }
        public string Selector { get; private set; }

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

        public SubscriptionIdentifier(string selector)
        {
            Selector = selector;

            var regex = new Regex(@"Topic = '(?<topic>[^\s]+)'( and)?( ?)(?<filter>.*)");
            var match = regex.Match(selector);

            Topic = match.Groups["topic"].Value;
            Filter = match.Groups["filter"].Value;
        }

        public override bool Equals(object obj)
        {
            SubscriptionIdentifier other = obj as SubscriptionIdentifier;
            return other != null && other.Selector == Selector;
        }

        public override int GetHashCode()
        {
            return Selector.GetHashCode();
        }

        public override string ToString()
        {
            return Selector;
        }
    }
}