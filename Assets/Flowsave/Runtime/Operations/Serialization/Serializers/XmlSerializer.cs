using System;
using System.IO;

namespace FlowSave.Serialization
{
    public class XmlSerializer : ISerializer
    {
        public SerializationType Format { get; } = SerializationType.Xml;

        public Result<byte[]> Serialize<T>(T data)
        {
            try
            {
                using var ms = new MemoryStream();
                var xml = new System.Xml.Serialization.XmlSerializer(typeof(T));
                xml.Serialize(ms, data);
                return Result<byte[]>.Success(ms.ToArray());
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"XML serialize failed: {ex.Message}");
            }
        }

        public Result<T> Deserialize<T>(byte[] data)
        {
            try
            {
                using var ms = new MemoryStream(data);
                var xml = new System.Xml.Serialization.XmlSerializer(typeof(T));
                T obj = (T)xml.Deserialize(ms);
                return Result<T>.Success(obj);
            }
            catch (Exception ex)
            {
                return Result<T>.Failure($"XML deserialize failed: {ex.Message}");
            }
        }
    }
}
