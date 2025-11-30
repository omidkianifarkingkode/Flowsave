using System;

namespace FlowSave.Signing
{
    [Serializable]
    public class DefaultSigningOptions
    {
        public SigningType SigningType = SigningType.None;

        public string HmacKeyId = "hmac-main";
    }

    [Serializable]
    public class SigningOptions : DefaultSigningOptions
    {
        public bool UseDefault = true;

        public static SigningOptions Clone(DefaultSigningOptions from) =>
            from == null ? null : new SigningOptions
            {
                UseDefault = true,
                SigningType = from.SigningType,
                HmacKeyId = from.HmacKeyId
            };

        public static SigningOptions Clone(SigningOptions from) =>
            from == null ? null : new SigningOptions
            {
                UseDefault = from.UseDefault,
                SigningType = from.SigningType,
                HmacKeyId = from.HmacKeyId
            };
    }
}
