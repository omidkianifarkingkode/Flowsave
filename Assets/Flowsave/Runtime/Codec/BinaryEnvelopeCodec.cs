using FlowSave.Operations;
using FlowSave.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FlowSave.Codec
{
    public static class EnvelopeConstants
    {
        // 'F','S','V','1' in little endian -> you'll see "1VSF" in a text viewer
        public const uint FileSignature = 0x31565346; // 'F' 'S' 'V' '1'
        public const byte CurrentEnvelopeVersion = 1;
    }

    public sealed class BinaryEnvelopeCodec : IEnvelopeCodec
    {
        public Result<byte[]> Encode(Envelope envelope)
        {
            if (envelope == null)
                return Result<byte[]>.Failure("Envelope is null.");

            try
            {
                using var ms = new MemoryStream();
                using (var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true))
                {
                    // --- Header / identity ---
                    bw.Write(EnvelopeConstants.FileSignature);    // uint
                    bw.Write(envelope.EnvelopeVersion);           // byte

                    WriteString(bw, envelope.NamespaceId);
                    bw.Write(envelope.DataVersion);

                    bw.Write((byte)envelope.PayloadFormat);       // SerializationType as byte

                    // --- Creator ---
                    if (envelope.Creator != null)
                    {
                        bw.Write(true); // hasCreator
                        WriteCreator(bw, envelope.Creator);
                    }
                    else
                    {
                        bw.Write(false); // hasCreator = false
                    }

                    // --- Operations ---
                    if (envelope.Operations != null && envelope.Operations.Count > 0)
                    {
                        bw.Write(envelope.Operations.Count);
                        foreach (var op in envelope.Operations)
                        {
                            bw.Write((byte)op.Kind); // OperationMode as byte
                            WriteString(bw, op.AlgorithmId);
                            WriteString(bw, op.KeyId);

                            if (op.Parameters != null && op.Parameters.Count > 0)
                            {
                                bw.Write(op.Parameters.Count);
                                foreach (var kv in op.Parameters)
                                {
                                    WriteString(bw, kv.Key);
                                    WriteString(bw, kv.Value);
                                }
                            }
                            else
                            {
                                bw.Write(0); // no parameters
                            }
                        }
                    }
                    else
                    {
                        bw.Write(0); // operations count
                    }

                    // --- Signature ---
                    if (envelope.Signature != null && envelope.Signature.Value != null)
                    {
                        bw.Write(true); // hasSignature
                        WriteString(bw, envelope.Signature.AlgorithmId);
                        WriteString(bw, envelope.Signature.KeyId);

                        var sig = envelope.Signature.Value;
                        bw.Write(sig.Length);
                        bw.Write(sig);
                    }
                    else
                    {
                        bw.Write(false); // hasSignature = false
                    }

                    // --- Payload ---
                    var payload = envelope.Payload ?? Array.Empty<byte>();
                    bw.Write(payload.Length);
                    bw.Write(payload);
                }

                return Result<byte[]>.Success(ms.ToArray());
            }
            catch (Exception ex)
            {
                return Result<byte[]>.Failure($"Binary encode failed: {ex.Message}");
            }
        }

        public Result<Envelope> Decode(byte[] data)
        {
            if (data == null)
                return Result<Envelope>.Failure("Data is null.");

            try
            {
                using var ms = new MemoryStream(data);
                using var br = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);

                // --- Header / identity ---
                var sig = br.ReadUInt32();
                if (sig != EnvelopeConstants.FileSignature)
                    return Result<Envelope>.Failure("Invalid envelope file signature.");

                var envVersion = br.ReadByte();
                if (envVersion > EnvelopeConstants.CurrentEnvelopeVersion)
                    return Result<Envelope>.Failure($"Unsupported envelope version: {envVersion}");

                var nsId = ReadString(br);
                var dataVersion = br.ReadInt32();

                var payloadFormatByte = br.ReadByte();
                var payloadFormat = (SerializationType)payloadFormatByte;

                // --- Creator ---
                CreatorInfo creator = null;
                bool hasCreator = br.ReadBoolean();
                if (hasCreator)
                {
                    creator = ReadCreator(br);
                }

                // --- Operations ---
                var operations = new List<OperationDescriptor>();
                int opCount = br.ReadInt32();
                if (opCount < 0)
                    return Result<Envelope>.Failure("Negative operations count.");

                for (int i = 0; i < opCount; i++)
                {
                    var kindByte = br.ReadByte();
                    var kind = (OperationMode)kindByte;

                    var algId = ReadString(br);
                    var keyId = ReadString(br);

                    int paramCount = br.ReadInt32();
                    if (paramCount < 0)
                        return Result<Envelope>.Failure("Negative operation parameter count.");

                    Dictionary<string, string> parameters = null;
                    if (paramCount > 0)
                    {
                        parameters = new Dictionary<string, string>(paramCount);
                        for (int p = 0; p < paramCount; p++)
                        {
                            var k = ReadString(br);
                            var v = ReadString(br);
                            parameters[k] = v;
                        }
                    }

                    operations.Add(new OperationDescriptor
                    {
                        Kind = kind,
                        AlgorithmId = algId,
                        KeyId = keyId,
                        Parameters = parameters
                    });
                }

                // --- Signature ---
                SignatureBlock signature = null;
                bool hasSignature = br.ReadBoolean();
                if (hasSignature)
                {
                    var algId = ReadString(br);
                    var keyId = ReadString(br);

                    int sigLen = br.ReadInt32();
                    if (sigLen < 0 || sigLen > ms.Length - ms.Position)
                        return Result<Envelope>.Failure("Invalid signature length in envelope.");

                    var sigBytes = br.ReadBytes(sigLen);
                    if (sigBytes.Length != sigLen)
                        return Result<Envelope>.Failure("Truncated signature bytes in envelope.");

                    signature = new SignatureBlock
                    {
                        AlgorithmId = algId,
                        KeyId = keyId,
                        Value = sigBytes
                    };
                }

                // --- Payload ---
                int payloadLen = br.ReadInt32();
                if (payloadLen < 0 || payloadLen > ms.Length - ms.Position)
                    return Result<Envelope>.Failure("Invalid payload length in envelope.");

                var payload = br.ReadBytes(payloadLen);
                if (payload.Length != payloadLen)
                    return Result<Envelope>.Failure("Truncated payload in envelope.");

                var env = new Envelope
                {
                    FileSignature = sig,
                    EnvelopeVersion = envVersion,
                    NamespaceId = nsId,
                    DataVersion = dataVersion,
                    PayloadFormat = payloadFormat,
                    Creator = creator,
                    Operations = operations,
                    Signature = signature,
                    Payload = payload
                };

                return Result<Envelope>.Success(env);
            }
            catch (EndOfStreamException eos)
            {
                return Result<Envelope>.Failure($"Binary decode failed: {eos.Message}");
            }
            catch (Exception ex)
            {
                return Result<Envelope>.Failure($"Binary decode failed: {ex.Message}");
            }
        }

        // ------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------

        private static void WriteString(BinaryWriter bw, string value)
        {
            if (value == null)
            {
                bw.Write((byte)0); // isNull = 0
                return;
            }

            bw.Write((byte)1); // isNull = 1
            bw.Write(value);
        }

        private static string ReadString(BinaryReader br)
        {
            byte isNonNull = br.ReadByte();
            if (isNonNull == 0)
                return null;

            return br.ReadString();
        }

        private static void WriteCreator(BinaryWriter bw, CreatorInfo c)
        {
            bw.Write(c.CreatedAtUtc.ToBinary());
            bw.Write(c.UpdatedAtUtc.ToBinary());

            WriteString(bw, c.AppVersion);
            WriteString(bw, c.BuildId);
            WriteString(bw, c.DeviceId);
        }

        private static CreatorInfo ReadCreator(BinaryReader br)
        {
            var createdTicks = br.ReadInt64();
            var updatedTicks = br.ReadInt64();

            var appVersion = ReadString(br);
            var buildId = ReadString(br);
            var deviceId = ReadString(br);

            return new CreatorInfo
            {
                CreatedAtUtc = DateTime.FromBinary(createdTicks),
                UpdatedAtUtc = DateTime.FromBinary(updatedTicks),
                AppVersion = appVersion,
                BuildId = buildId,
                DeviceId = deviceId
            };
        }
    }
}
