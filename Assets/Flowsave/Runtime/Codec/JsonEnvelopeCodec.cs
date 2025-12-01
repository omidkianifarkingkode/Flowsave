using Flowsave.Codec;
using Newtonsoft.Json;
using System;
using System.Text;

namespace FlowSave.Codec
{
    public sealed class JsonEnvelopeCodec : IEnvelopeCodec
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include
        };

        public Result<byte[]> Encode(Envelope envelope)
        {
            try
            {
                if (envelope == null)
                    return Result<byte[]>.Failure("Envelope is null.");

                string json = JsonConvert.SerializeObject(envelope, Settings);
                return Result<byte[]>.Success(Encoding.UTF8.GetBytes(json));
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"JSON encode failed: {ex.Message}");
            }
        }

        public Result<Envelope> Decode(byte[] data)
        {
            try
            {
                if (data == null)
                    return Result<Envelope>.Failure("Data is null.");

                string json = Encoding.UTF8.GetString(data);
                var env = JsonConvert.DeserializeObject<Envelope>(json, Settings);

                if (env == null)
                    return Result<Envelope>.Failure("Deserialized envelope is null.");

                return Result<Envelope>.Success(env);
            }
            catch (Exception ex)
            {
                return Result<Envelope>.Failure($"JSON decode failed: {ex.Message}");
            }
        }
    }
}
