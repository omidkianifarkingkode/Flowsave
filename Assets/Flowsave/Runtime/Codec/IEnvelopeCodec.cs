using Flowsave.Codec;

namespace FlowSave.Codec
{
    public interface IEnvelopeCodec
    {
        Result<byte[]> Encode(Envelope envelope);
        Result<Envelope> Decode(byte[] data);
    }
}
