using Flowsave.Configurations;
using System.IO;

namespace FlowSave
{
    public class XmlSerializer : ISerializer
    {
        public SerializerType Format { get; } = SerializerType.Xml;

        public byte[] Serialize<T>(T data)
        {
            using (var memoryStream = new MemoryStream())
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                serializer.Serialize(memoryStream, data);
                return memoryStream.ToArray();
            }
        }

        public T Deserialize<T>(byte[] data)
        {
            using (var memoryStream = new MemoryStream(data))
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                return (T)serializer.Deserialize(memoryStream);
            }
        }
    }
}
