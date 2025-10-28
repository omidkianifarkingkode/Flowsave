using Flowsave.Shared;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Flowsave.Serialization
{
    public class BinarySerializer : ISerializer
    {
        public SerializerType Format { get; } = SerializerType.Binary;

        public byte[] Serialize<T>(T data)
        {
            using var memoryStream = new MemoryStream();

            var formatter = new BinaryFormatter();
            formatter.Serialize(memoryStream, data);
            return memoryStream.ToArray();
        }

        public T Deserialize<T>(byte[] data)
        {
            using var memoryStream = new MemoryStream(data);

            var formatter = new BinaryFormatter();
            return (T)formatter.Deserialize(memoryStream);
        }
    }
}
