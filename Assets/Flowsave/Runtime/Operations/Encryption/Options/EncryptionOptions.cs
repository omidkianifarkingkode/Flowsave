using System;
using UnityEngine;

namespace Flowsave.Security
{
    [Serializable]
    public class DefaultEncryptionOptions
    {
        public EncryptionType EncryptionType = EncryptionType.None;

        [Tooltip("AES encryption options.")]
        public AesOptions Aes128 = new();

        [Tooltip("AES encryption options.")]
        public AesOptions Aes256 = new();
    }

    [Serializable]
    public class EncryptionOptions : DefaultEncryptionOptions
    {
        public bool UseDefault = true;

        public static EncryptionOptions Clone(DefaultEncryptionOptions from) =>
            from == null ? null : new EncryptionOptions
            {
                UseDefault = true,

                EncryptionType = from.EncryptionType,
                Aes128 = AesOptions.Clone(from.Aes128),
                Aes256 = AesOptions.Clone(from.Aes256)
            };

        public static EncryptionOptions Clone(EncryptionOptions from) =>
            from == null ? null : new EncryptionOptions
            {
                UseDefault = from.UseDefault,
                EncryptionType = from.EncryptionType,
                Aes128 = AesOptions.Clone(from.Aes128),
                Aes256 = AesOptions.Clone(from.Aes256)
            };

    }
}
