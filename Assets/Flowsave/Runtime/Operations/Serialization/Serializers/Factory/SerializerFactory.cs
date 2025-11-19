using Flowsave.Namespaces;
using System;

namespace Flowsave.Serialization
{
    public class SerializerFactory : ISerializerFactory
    {
        private readonly SerializationOptions _options;

        public SerializerFactory(SerializationOptions options)
        {
            _options = options;
        }

        public ISerializer CreateSerializer(SerializationType serializerType)
        {
            return serializerType switch
            {
                SerializationType.Json => new JsonSerializer(_options.Json),
#pragma warning disable SYSLIB0011
                SerializationType.Binary_Legacy => new LegacyBinaryFormatterSerializer(),
#pragma warning restore SYSLIB0011
#if FLOWSAVE_PROTOBUF_NET
                SerializerType.Binary_Protobuf => new ProtobufSerializer(),
#endif
#if FLOWSAVE_MessagePack
                SerializerType.Binary_MessagePack => new MessagePackBinarySerializer(),
#endif
                SerializationType.Xml => new XmlSerializer(),
                _ => throw new InvalidOperationException($"Unsupported serializer type: {serializerType}")
            };
        }
    }
}
