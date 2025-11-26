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
                SerializationType.None => new NoOpSerializer(),
                SerializationType.Json => new JsonSerializer(_options.Json),
#pragma warning disable CS0618
                SerializationType.Binary_Legacy => new LegacyBinaryFormatterSerializer(),
#pragma warning restore CS0618
#if FLOWSAVE_PROTOBUF_NET
                SerializationType.Binary_Protobuf => new ProtobufSerializer(),
#endif
#if FLOWSAVE_MESSAGEPACK
                SerializationType.Binary_MessagePack => new MessagePackBinarySerializer(),
#endif
                SerializationType.Xml => new XmlSerializer(),
                SerializationType.Csv => new CsvSerializer(),
                _ => throw new InvalidOperationException($"Unsupported serializer type: {serializerType}")
            };
        }
    }
}
