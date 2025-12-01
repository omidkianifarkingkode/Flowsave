using FlowSave.Serialization;
using System.Collections.Generic;

namespace Flowsave.Codec
{
    public sealed class Envelope
    {
        // --- Header / identity ---
        public uint FileSignature { get; set; }     // e.g. 'F','S','V','1' => 0x46535631
        public byte EnvelopeVersion { get; set; }   // for envelope format itself

        public string NamespaceId { get; set; }     // e.g. "profile", "settings"
        public int DataVersion { get; set; }        // your logical save version

        // --- Payload encoding ---
        public SerializationType PayloadFormat { get; set; } // Json/Binary/MsgPack...

        // --- Creator / origin info (optional, but very useful) ---
        public CreatorInfo Creator { get; set; }

        // --- Transformation pipeline (compress/encrypt/etc.) ---
        public List<OperationDescriptor> Operations { get; set; }

        // --- Signature / integrity (optional) ---
        public SignatureBlock Signature { get; set; }

        // --- Final bytes after all operations ---
        public byte[] Payload { get; set; }
    }
}
