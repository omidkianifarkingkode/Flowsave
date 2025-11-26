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

        public Result<byte[]> Serialize<T>(T data)
        {
            try
            {
                using var ms = new MemoryStream();
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, data);
                return Result<byte[]>.Success(ms.ToArray());
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"BinaryFormatter serialize failed: {ex.Message}");
            }
        }

        public Result<T> Deserialize<T>(byte[] data)
        {
            try
            {
                using var ms = new MemoryStream(data);
                var formatter = new BinaryFormatter();
                T obj = (T)formatter.Deserialize(ms);
                return Result<T>.Success(obj);
            }
            catch (Exception ex)
            {
                return Result<T>.Failure($"BinaryFormatter deserialize failed: {ex.Message}");
            }
        }
    }
}
