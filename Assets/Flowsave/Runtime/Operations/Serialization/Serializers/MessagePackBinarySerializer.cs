#if FLOWSAVE_MESSAGEPACK

using MessagePack;

namespace Flowsave.Serialization
{
    /// <summary>
    /// Binary serializer using MessagePack.
    /// </summary>
    public class MessagePackBinarySerializer : ISerializer
    {
        public SerializationType Format { get; } = SerializationType.Binary_MessagePack;

        public byte[] Serialize<T>(T data)
        {
            return MessagePackSerializer.Serialize(data);
        }

        public T Deserialize<T>(byte[] data)
        {
            return MessagePackSerializer.Deserialize<T>(data);
        }
    }
}
#endif
