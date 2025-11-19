using System;
using UnityEngine;

namespace Flowsave.Security
{
    [Serializable]
    public class EncryptionOptions 
    {
        [field: SerializeField] public EncryptionType EncryptionType { get; private set; } = EncryptionType.None;
        [field: SerializeField] public bool UseDefault { get; private set; } = true;

        [Tooltip("AES encryption options.")]
        [field: SerializeField] public AesOptions Aes128 { get; private set; } = new AesOptions();

        [Tooltip("AES encryption options.")]
        [field: SerializeField] public AesOptions Aes256 { get; private set; } = new AesOptions();
    }
}
