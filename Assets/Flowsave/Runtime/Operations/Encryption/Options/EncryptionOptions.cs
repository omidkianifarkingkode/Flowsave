using System;
using UnityEngine;

namespace FlowSave.Encryption
{
    [Serializable]
    public class DefaultEncryptionOptions
    {
        public EncryptionType EncryptionType = EncryptionType.None;

        [Tooltip("Key id for AES-128 CBC in this environment.")]
        public string Aes128KeyId = "aes-main";

        [Tooltip("Key id for AES-256 CBC in this environment.")]
        public string Aes256KeyId = "aes-main";
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
                Aes128KeyId = from.Aes128KeyId,
                Aes256KeyId = from.Aes256KeyId
            };

        public static EncryptionOptions Clone(EncryptionOptions from) =>
            from == null ? null : new EncryptionOptions
            {
                UseDefault = from.UseDefault,
                EncryptionType = from.EncryptionType,
                Aes128KeyId = from.Aes128KeyId,
                Aes256KeyId = from.Aes256KeyId
            };
    }
}
