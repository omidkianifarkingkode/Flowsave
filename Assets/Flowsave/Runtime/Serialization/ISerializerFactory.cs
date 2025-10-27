using Flowsave.Configurations;
using System;

namespace FlowSave
{
    public interface ISerializerFactory
    {
        ISerializer CreateSerializer(SerializerType serializerType);
    }

    public class SerializerFactory : ISerializerFactory
    {
        public ISerializer CreateSerializer(SerializerType serializerType)
        {
            return serializerType switch
            {
                SerializerType.Json => new JsonSerializer(),
                SerializerType.Binary => new BinarySerializer(),
                SerializerType.Xml => new XmlSerializer(),
                _ => throw new InvalidOperationException($"Unsupported serializer type: {serializerType}")
            };
        }
    }
}
