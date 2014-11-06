using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Newtonsoft.Json.Serialization;

namespace SignalR.MagicHub.Serialization
{

    // todo: This needs to live in Commons eventually. Currently it's duplicated in E&B Service
    //https://gist.github.com/roryf/1042502
    public class SnakeCasePropertyNamesContractResolver : DeliminatorSeparatedPropertyNamesContractResolver
    {
        public SnakeCasePropertyNamesContractResolver() : base('_') { }
    }

    public class DeliminatorSeparatedPropertyNamesContractResolver : DefaultContractResolver
    {
        private readonly string _separator;

        protected DeliminatorSeparatedPropertyNamesContractResolver(char separator)
            : base(true)
        {
            _separator = separator.ToString(CultureInfo.InvariantCulture);
        }

        protected override string ResolvePropertyName(string propertyName)
        {
            var parts = new List<string>();
            var currentWord = new StringBuilder();
            var foundPeriod = false;

            foreach (var c in propertyName)
            {
                if (char.IsUpper(c) && currentWord.Length > 0 && !foundPeriod)
                {
                    parts.Add(currentWord.ToString());
                    currentWord.Clear();
                }
                currentWord.Append(char.ToLower(c));

                foundPeriod = (c == '.');

            }

            if (currentWord.Length > 0)
            {
                parts.Add(currentWord.ToString());
            }

            return string.Join(_separator, parts.ToArray());
        }
    }
}