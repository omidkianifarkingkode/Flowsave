using System;
using UnityEngine;

namespace Flowsave.Security.Options
{
    [Serializable]
    public class SigningOptions 
    {
        [field: SerializeField] public SigningType SigningType { get; private set; } = SigningType.None;
        [field: SerializeField] public bool UseDefault { get; private set; } = true;

        [field: SerializeField] public HmacOptions Hmac { get; private set; } = new HmacOptions();
    }
}
