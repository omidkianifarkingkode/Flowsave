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

        public Result<byte[]> Serialize<T>(T data)
        {
            try
            {
                using var ms = new MemoryStream();
                Serializer.Serialize(ms, data);
                return Result<byte[]>.Success(ms.ToArray());
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"Protobuf serialize failed: {ex.Message}");
            }
        }

        public Result<T> Deserialize<T>(byte[] data)
        {
            try
            {
                using var ms = new MemoryStream(data);
                T obj = Serializer.Deserialize<T>(ms);
                return Result<T>.Success(obj);
            }
            catch (Exception ex)
            {
                return Result<T>.Failure($"Protobuf deserialize failed: {ex.Message}");
            }
        }
    }
}
#endif
