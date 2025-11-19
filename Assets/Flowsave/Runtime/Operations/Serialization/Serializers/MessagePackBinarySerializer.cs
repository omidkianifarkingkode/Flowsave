#if FLOWSAVE_MessagePack

using Flowsave.Shared;
using MessagePack;

namespace Flowsave.Serialization
{
    /// <summary>
    /// Binary serializer using MessagePack.
    /// </summary>
    public class MessagePackBinarySerializer : ISerializer
    {
        public SerializerType Format { get; } = SerializerType.Binary_MessagePack;

        public byte[] Serialize<T>(T data)
        {
            return MessagePackSerializer.Serialize(data);
        }

        public T Deserialize<T>(byte[] data)
        {
            return  MessagePackSerializer.Deserialize<T>(data);
        }
    }
}
#endif
