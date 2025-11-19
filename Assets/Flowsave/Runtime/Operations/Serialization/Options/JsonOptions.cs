using System;
using UnityEngine;

namespace Flowsave.Serialization
{
    [Serializable]
    public class JsonOptions
    {
        [field: SerializeField] public bool PrettyPrint { get; private set; } = false;
        [field: SerializeField] public bool IncludeNulls { get; private set; } = true;
        [Tooltip("True for full type hinting, false for simple")]
        [field: SerializeField] public bool TypeHinting { get; private set; } = true;
    }
}