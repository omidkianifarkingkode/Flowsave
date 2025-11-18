namespace Flowsave.Serialization
{
    public enum SerializationType
    {
        Json = 1,
        Binary_Legacy = 2,
#if FLOWSAVE_PROTOBUF_NET
        Binary_Protobuf = 3,
#endif
#if FLOWSAVE_MessagePack
        Binary_MessagePack = 4,
#endif
        Xml = 5,
        Csv = 6,
        Custom = 7
    }

}