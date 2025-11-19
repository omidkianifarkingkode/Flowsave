using System;
using UnityEngine;

namespace Flowsave.Serialization
{
    [Serializable]
    public class SerializationOptions
    {
        [field: SerializeField] public SerializationType SerializationType { get; private set; } = SerializationType.Json;
        [field: SerializeField] public bool UseDefault { get; private set; } = true;

        [field: SerializeField] public JsonOptions Json { get; private set; }
    }
}