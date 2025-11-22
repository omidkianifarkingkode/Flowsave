using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Flowsave.Serialization
{
    /// <summary>
    /// Legacy BinaryFormatter-based serializer.
    /// DO NOT USE in new code. Kept only for backward compatibility with old save files.
    /// </summary>
    [Obsolete("BinaryFormatter is insecure and obsolete. Use MessagePackBinarySerializer or ProtobufBinarySerializer instead.", error: false)]
    public class LegacyBinaryFormatterSerializer : ISerializer
    {
        public SerializationType Format { get; } = SerializationType.Binary_Legacy;

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
