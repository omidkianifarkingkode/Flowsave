using Flowsave.Codec;
using FlowSave.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FlowSave.Codec
{
    public sealed class BinaryEnvelopeCodec : IEnvelopeCodec
    {
        public Result<byte[]> Encode(Envelope envelope)
        {
            try
            {
                if (envelope == null)
                    return Result<byte[]>.Failure("Envelope is null.");

                using var ms = new MemoryStream();
                using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

                WriteEnvelope(writer, envelope);
                writer.Flush();

                return Result<byte[]>.Success(ms.ToArray());
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"Binary encode failed: {ex.Message}");
            }
        }

        public Result<Envelope> Decode(byte[] data)
        {
            try
            {
                if (data == null)
                    return Result<Envelope>.Failure("Data is null.");

                using var ms = new MemoryStream(data);
                using var reader = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);

                var env = ReadEnvelope(reader);
                return Result<Envelope>.Success(env);
            }
            catch (Exception ex)
            {
                return Result<Envelope>.Failure($"Binary decode failed: {ex.Message}");
            }
        }

        // ----------------- write -----------------

        private static void WriteEnvelope(BinaryWriter w, Envelope e)
        {
            w.Write(EnvelopeConstants.FileSignature);
            w.Write(EnvelopeConstants.CurrentEnvelopeVersion);

            WriteString(w, e.NamespaceId);
            w.Write(e.DataVersion);

            w.Write((byte)e.PayloadFormat);

            // Creator
            if (e.Creator != null)
            {
                w.Write(true);
                w.Write(e.Creator.CreatedAtUtc.Ticks);
                w.Write(e.Creator.UpdatedAtUtc.Ticks);
                WriteString(w, e.Creator.AppVersion);
                WriteString(w, e.Creator.BuildId);
                WriteString(w, e.Creator.DeviceId);
            }
            else
            {
                w.Write(false);
            }

            // Operations
            var ops = e.Operations ?? new List<OperationDescriptor>();
            w.Write(ops.Count);
            foreach (var op in ops)
            {
                WriteString(w, op.Kind);
                WriteString(w, op.AlgorithmId);
                WriteString(w, op.KeyId);

                var parameters = op.Parameters ?? new Dictionary<string, string>();
                w.Write(parameters.Count);
                foreach (var kv in parameters)
                {
                    WriteString(w, kv.Key);
                    WriteString(w, kv.Value);
                }
            }

            // Signature
            if (e.Signature != null)
            {
                w.Write(true);
                WriteString(w, e.Signature.AlgorithmId);
                WriteString(w, e.Signature.KeyId);

                var value = e.Signature.Value ?? Array.Empty<byte>();
                w.Write(value.Length);
                w.Write(value);
            }
            else
            {
                w.Write(false);
            }

            // Payload
            var payload = e.Payload ?? Array.Empty<byte>();
            w.Write(payload.Length);
            w.Write(payload);
        }

        // ----------------- read -----------------

        private static Envelope ReadEnvelope(BinaryReader r)
        {
            var sig = r.ReadUInt32();
            if (sig != EnvelopeConstants.FileSignature)
                throw new InvalidDataException($"Invalid envelope signature: 0x{sig:X8}");

            var version = r.ReadByte();
            if (version != EnvelopeConstants.CurrentEnvelopeVersion)
            {
                // For now we only support v1. Later you can branch here.
                throw new NotSupportedException($"Unsupported envelope version: {version}");
            }

            var env = new Envelope
            {
                FileSignature = sig,
                EnvelopeVersion = version
            };

            env.NamespaceId = ReadString(r);
            env.DataVersion = r.ReadInt32();

            env.PayloadFormat = (SerializationType)r.ReadByte();

            // Creator
            if (r.ReadBoolean())
            {
                var creator = new CreatorInfo
                {
                    CreatedAtUtc = new DateTime(r.ReadInt64(), DateTimeKind.Utc),
                    UpdatedAtUtc = new DateTime(r.ReadInt64(), DateTimeKind.Utc),
                    AppVersion = ReadString(r),
                    BuildId = ReadString(r),
                    DeviceId = ReadString(r)
                };
                env.Creator = creator;
            }

            // Operations
            int opCount = r.ReadInt32();
            var ops = new List<OperationDescriptor>(opCount);
            for (int i = 0; i < opCount; i++)
            {
                var op = new OperationDescriptor
                {
                    Kind = ReadString(r),
                    AlgorithmId = ReadString(r),
                    KeyId = ReadString(r),
                    Parameters = ReadParameters(r)
                };
                ops.Add(op);
            }
            env.Operations = ops;

            // Signature
            if (r.ReadBoolean())
            {
                var sigBlock = new SignatureBlock
                {
                    AlgorithmId = ReadString(r),
                    KeyId = ReadString(r)
                };
                int len = r.ReadInt32();
                sigBlock.Value = r.ReadBytes(len);
                env.Signature = sigBlock;
            }

            // Payload
            int payloadLen = r.ReadInt32();
            env.Payload = r.ReadBytes(payloadLen);

            return env;
        }

        // ----------------- helpers -----------------

        private static void WriteString(BinaryWriter w, string value)
        {
            if (value == null)
            {
                w.Write((byte)0); // 0 length = null marker
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(value);
            // store length as Int32; 0 reserved for null, so add 1
            w.Write((int)(bytes.Length + 1));
            w.Write(bytes);
        }

        private static string ReadString(BinaryReader r)
        {
            int len = r.ReadInt32();
            if (len == 0)
                return null;

            int byteLen = len - 1;
            var bytes = r.ReadBytes(byteLen);
            return bytes.Length == 0 ? string.Empty : Encoding.UTF8.GetString(bytes);
        }

        private static Dictionary<string, string> ReadParameters(BinaryReader r)
        {
            int count = r.ReadInt32();
            var dict = new Dictionary<string, string>(count);
            for (int i = 0; i < count; i++)
            {
                var key = ReadString(r);
                var value = ReadString(r);
                dict[key] = value;
            }
            return dict;
        }
    }
}
