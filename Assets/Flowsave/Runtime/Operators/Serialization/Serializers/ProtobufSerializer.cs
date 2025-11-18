#if FLOWSAVE_PROTOBUF_NET
using System;
using System.IO;
using Flowsave.Shared;
using ProtoBuf;

namespace Flowsave.Serialization
{
    /// <summary>
    /// Binary serializer using protobuf-net.
    /// </summary>
    public class ProtobufSerializer : ISerializer
    {
        public SerializerType Format => SerializerType.Binary_Protobuf;

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
