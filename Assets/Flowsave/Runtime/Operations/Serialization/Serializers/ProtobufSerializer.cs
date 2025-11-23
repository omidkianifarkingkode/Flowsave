#if FLOWSAVE_PROTOBUF_NET
using ProtoBuf;
using System;
using System.IO;

namespace Flowsave.Serialization
{
    /// <summary>
    /// Binary serializer using protobuf-net.
    /// </summary>
    public class ProtobufSerializer : ISerializer
    {
        public SerializationType Format => SerializationType.Binary_Protobuf;

        public byte[] Serialize<T>(T data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            using var ms = new MemoryStream();
            Serializer.Serialize(ms, data);
            return ms.ToArray();
        }

        public T Deserialize<T>(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            using var ms = new MemoryStream(data);
            return Serializer.Deserialize<T>(ms);
        }
    }
}
#endif
