using System;
using UnityEngine;

namespace Flowsave.Serialization
{
    [Serializable]
    public class JsonOptions
    {
        public bool PrettyPrint = false;
        public bool IncludeNulls = true;
        [Tooltip("True for full type hinting, false for simple")]
        public bool TypeHinting = true;

        public static JsonOptions Clone(JsonOptions from) =>
            from == null ? null : new JsonOptions
            {
                PrettyPrint = from.PrettyPrint,
                IncludeNulls = from.IncludeNulls,
                TypeHinting = from.TypeHinting
            };
    }
}