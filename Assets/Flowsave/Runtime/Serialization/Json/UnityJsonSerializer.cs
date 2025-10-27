using System;
using System.Text;
using UnityEngine;

namespace FlowSave
{
    /// <summary>
    /// Serializer implementation that delegates to <see cref="JsonUtility"/>.
    /// </summary>
    public class UnityJsonSerializer : ISerializer
    {
        private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

        public string Format => "json";

        public byte[] Serialize<T>(T data)
        {
            if (data == null)
            {
                return Array.Empty<byte>();
            }

            string json = JsonUtility.ToJson(data);
            return Utf8NoBom.GetBytes(json);
        }

        public T Deserialize<T>(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return default;
            }

            string json = Utf8NoBom.GetString(data);
            return JsonUtility.FromJson<T>(json);
        }
    }
}
