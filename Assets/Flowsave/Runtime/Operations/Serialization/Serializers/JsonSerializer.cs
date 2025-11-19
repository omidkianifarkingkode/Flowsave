using Newtonsoft.Json;
using System.Text;

namespace Flowsave.Serialization
{
    public class JsonSerializer : ISerializer
    {
        public SerializationType Format { get; } = SerializationType.Json;

        private readonly JsonSerializerSettings _settings;

        public JsonSerializer(JsonOptions options)
        {
            _settings = new JsonSerializerSettings
            {
                Formatting = options.PrettyPrint ? Formatting.Indented : Formatting.None,
                NullValueHandling = options.IncludeNulls ? NullValueHandling.Include : NullValueHandling.Ignore,
                TypeNameHandling = options.TypeHinting
                    ? TypeNameHandling.All       // full type hinting
                    : TypeNameHandling.Auto      // minimal
            };
        }

        public byte[] Serialize<T>(T data)
        {
            var json = JsonConvert.SerializeObject(data, _settings);
            return Encoding.UTF8.GetBytes(json);
        }

        public T Deserialize<T>(byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<T>(json, _settings);
        }
    }
}
