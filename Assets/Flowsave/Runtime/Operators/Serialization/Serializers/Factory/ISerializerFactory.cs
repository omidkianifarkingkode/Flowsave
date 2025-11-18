using Flowsave.Shared;

namespace Flowsave.Serialization
{
    public interface ISerializerFactory
    {
        ISerializer CreateSerializer(SerializationType serializerType);
    }
}
