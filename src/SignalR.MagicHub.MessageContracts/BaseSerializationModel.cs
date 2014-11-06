using Newtonsoft.Json;
using SignalR.MagicHub.Serialization;

namespace SignalR.MagicHub.MessageContracts
{
    public abstract class BaseSerializationModel
    {
        private static readonly SnakeCasePropertyNamesContractResolver _resolver =
            new SnakeCasePropertyNamesContractResolver();

        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings()
            {
                ContractResolver = _resolver, NullValueHandling = NullValueHandling.Ignore
            };

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, _settings);
        }
    }
}