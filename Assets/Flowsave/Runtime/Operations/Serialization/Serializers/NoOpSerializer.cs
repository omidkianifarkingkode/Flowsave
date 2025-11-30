using System;
using System.Text;

namespace FlowSave.Serialization
{
    /// <summary>
    /// "No-op" serializer used when SerializationType.None is selected.
    /// 
    /// - For T == byte[]: pass-through (no-op).
    /// - For T == string: UTF8 encode/decode.
    /// - For any other type: returns Failure.
    /// </summary>
    public sealed class NoOpSerializer : ISerializer
    {
        public SerializationType Format => SerializationType.None;

        public Result<byte[]> Serialize<T>(T data)
        {
            // Handle null explicitly
            if (data == null)
                return Result<byte[]>.Failure("NoOpSerializer cannot serialize null (T is unknown).");

            // 1) T == byte[]  → passthrough
            if (data is byte[] bytes)
                return Result<byte[]>.Success(bytes);

            // 2) T == string  → UTF8 encode
            if (data is string s)
            {
                var utf8 = Encoding.UTF8.GetBytes(s);
                return Result<byte[]>.Success(utf8);
            }

            // 3) Anything else is unsupported
            return Result<byte[]>.Failure(
                $"SerializationType.None does not support serializing type '{typeof(T).FullName}'. " +
                "Use a real serializer (Json, MessagePack, etc.) or save raw bytes explicitly.");
        }

        public Result<T> Deserialize<T>(byte[] data)
        {
            if (data == null)
                return Result<T>.Failure("NoOpSerializer cannot deserialize null data.");

            // 1) T == byte[] → passthrough
            if (typeof(T) == typeof(byte[]))
            {
                // Option: return same array; if you want a copy use Array.Copy
                return Result<T>.Success((T)(object)data);
            }

            // 2) T == string → UTF8 decode
            if (typeof(T) == typeof(string))
            {
                var s = Encoding.UTF8.GetString(data);
                return Result<T>.Success((T)(object)s);
            }

            // 3) Anything else is unsupported
            return Result<T>.Failure(
                $"SerializationType.None does not support deserializing to type '{typeof(T).FullName}'. " +
                "Use a real serializer or load raw bytes/string instead.");
        }
    }
}
