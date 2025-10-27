using System;

namespace FlowSave
{
    /// <summary>
    /// Defines low level persistence operations for FlowSave contexts.
    /// </summary>
    public interface ISaveProvider
    {
        /// <summary>
        /// Persists a binary payload associated with the provided key.
        /// </summary>
        /// <param name="key">Identifier of the data record.</param>
        /// <param name="data">Binary payload to persist.</param>
        void Save(string key, byte[] data);

        /// <summary>
        /// Loads a previously persisted payload.
        /// </summary>
        /// <param name="key">Identifier of the data record.</param>
        /// <returns>The persisted binary payload.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the key does not exist.</exception>
        byte[] Load(string key);

        /// <summary>
        /// Removes a persisted payload associated with the provided key.
        /// </summary>
        /// <param name="key">Identifier of the data record.</param>
        void Delete(string key);

        /// <summary>
        /// Checks whether the provided key exists in the underlying storage.
        /// </summary>
        /// <param name="key">Identifier of the data record.</param>
        /// <returns>True if the key exists, otherwise false.</returns>
        bool Exists(string key);
    }
}
