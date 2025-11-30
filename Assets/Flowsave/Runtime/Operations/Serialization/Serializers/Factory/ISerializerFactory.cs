namespace FlowSave.Serialization
{
    public interface ISerializerFactory
    {
        ISerializer CreateSerializer(SerializationType serializerType);
    }
}
