using System;
using UnityEngine;

namespace Flowsave.Storage
{
    [Serializable]
    public class PlayerPrefsStorageOptions
    {
        /// <summary>
        /// Prefix added to all PlayerPrefs keys (namespacing).
        /// </summary>
        public string Prefix = "ds:";
        /// <summary>
        /// Chunk size in characters for Base64 data. 16k is a conservative default.
        /// </summary>
        public int ChunkChars = 16_384;
        /// <summary>
        /// If true, calls PlayerPrefs.Save() after each mutation.
        /// </summary>
        public bool AutoSave = true;

        public static PlayerPrefsStorageOptions Clone(PlayerPrefsStorageOptions from) =>
            from == null ? null : new PlayerPrefsStorageOptions
            {
                Prefix = from.Prefix,
                ChunkChars = from.ChunkChars,
                AutoSave = from.AutoSave
            };
    }
}
