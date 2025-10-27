using Flowsave.Configurations;
using System.Text;
using Unity.Plastic.Newtonsoft.Json;

namespace FlowSave
{
    public class JsonSerializer : ISerializer
    {
        public SerializerType Format { get; } = SerializerType.Json;

        public byte[] Serialize<T>(T data)
        {
            var json = JsonConvert.SerializeObject(data);
            return Encoding.UTF8.GetBytes(json);
        }

        public T Deserialize<T>(byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
