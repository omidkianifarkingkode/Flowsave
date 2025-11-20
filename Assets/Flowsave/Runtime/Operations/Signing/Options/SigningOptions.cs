using System;
using UnityEngine;

namespace Flowsave.Security.Options
{
    [Serializable]
    public class DefaultSigningOptions
    {
        public SigningType SigningType = SigningType.None;

        public HmacOptions Hmac = new();
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
                Hmac = HmacOptions.Clone(from.Hmac)
            };


        public static SigningOptions Clone(SigningOptions from) =>
            from == null ? null : new SigningOptions
            {
                UseDefault = from.UseDefault,
                SigningType = from.SigningType,
                Hmac = HmacOptions.Clone(from.Hmac)
            };

    }
}
