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
        [field:SerializeField] public string Prefix { get; set; } = "ds:";
        /// <summary>
        /// Chunk size in characters for Base64 data. 16k is a conservative default.
        /// </summary>
        [field: SerializeField] public int ChunkChars { get; set; } = 16_384;
        /// <summary>
        /// If true, calls PlayerPrefs.Save() after each mutation.
        /// </summary>
        [field: SerializeField] public bool AutoSave { get; set; } = true;
    }
}
