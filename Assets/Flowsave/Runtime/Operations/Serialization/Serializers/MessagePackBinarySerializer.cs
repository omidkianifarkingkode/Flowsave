#if FLOWSAVE_MESSAGEPACK

using MessagePack;
using System;

namespace Flowsave.Serialization
{
    /// <summary>
    /// Binary serializer using MessagePack.
    /// </summary>
    public class MessagePackBinarySerializer : ISerializer
    {
        public SerializationType Format { get; } = SerializationType.Binary_MessagePack;

        public Result<byte[]> Serialize<T>(T data)
        {
            try
            {
                return Result<byte[]>.Success(MessagePackSerializer.Serialize(data));
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"MessagePack serialize failed: {ex.Message}");
            }
        }

        public Result<T> Deserialize<T>(byte[] data)
        {
            try
            {
                return Result<T>.Success(MessagePackSerializer.Deserialize<T>(data));
            }
            catch (Exception ex)
            {
                return Result<T>.Failure($"MessagePack deserialize failed: {ex.Message}");
            }
        }
    }
}
#endif
