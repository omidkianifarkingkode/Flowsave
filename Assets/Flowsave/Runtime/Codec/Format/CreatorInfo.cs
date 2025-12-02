using System;

namespace FlowSave.Codec
{
    public sealed class CreatorInfo
    {
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }

        public string AppVersion { get; set; }      // "1.4.3"
        public string BuildId { get; set; }         // commit hash / CI build id
        public string DeviceId { get; set; }        // optional, if you want
    }
}
