using System;
using UnityEngine;

namespace FlowSave.Serialization
{
    [Serializable]
    public class DefaultSerializationOptions
    {
        public SerializationType SerializationType = SerializationType.Json;
        public JsonOptions Json = new();
    }

    [Serializable]
    public class SerializationOptions : DefaultSerializationOptions
    {
        public bool UseDefault = true;

        public static SerializationOptions Clone(DefaultSerializationOptions from) =>
            from == null ? null : new SerializationOptions
            {
                UseDefault = true,
                SerializationType = from.SerializationType,
                Json = JsonOptions.Clone(from.Json)
            };


        public static SerializationOptions Clone(SerializationOptions from) =>
            from == null ? null : new SerializationOptions
            {
                UseDefault = from.UseDefault,
                SerializationType = from.SerializationType,
                Json = JsonOptions.Clone(from.Json)
            };

    }
}