namespace FlowSave.Codec
{
    public static class EnvelopeCodecFactory
    {
        public static IEnvelopeCodec Create(EnvelopeCodecKind kind)
        {
            switch (kind)
            {
                case EnvelopeCodecKind.Json:
                    return new JsonEnvelopeCodec();
                case EnvelopeCodecKind.Binary:
                default:
                    return new BinaryEnvelopeCodec();
            }
        }

        // Optional helper for auto mode
        public static IEnvelopeCodec CreateAuto(bool devPreferJson)
        {
#if UNITY_EDITOR
            if (devPreferJson)
                return new JsonEnvelopeCodec();
#endif
            return new BinaryEnvelopeCodec();
        }
    }
}
