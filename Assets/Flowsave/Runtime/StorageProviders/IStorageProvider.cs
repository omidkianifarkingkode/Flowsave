using System.Threading.Tasks;

namespace FlowSave
{
    /// <summary>
    /// Defines a contract for data storage operations (asynchronous).
    /// </summary>
    public interface IStorageProvider
    {
        /// <summary>
        /// Persists a binary payload associated with the provided key.
        /// </summary>
        /// <param name="key">Identifier of the data record.</param>
        /// <param name="data">Binary payload to persist.</param>
        Task SaveAsync(string key, byte[] data);

        /// <summary>
        /// Loads a previously persisted payload.
        /// </summary>
        /// <param name="key">Identifier of the data record.</param>
        /// <returns>The persisted binary payload.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the key does not exist.</exception>
        Task<byte[]> LoadAsync(string key);

        /// <summary>
        /// Removes a persisted payload associated with the provided key.
        /// </summary>
        /// <param name="key">Identifier of the data record.</param>
        Task DeleteAsync(string key);

        /// <summary>
        /// Checks whether the provided key exists in the underlying storage.
        /// </summary>
        /// <param name="key">Identifier of the data record.</param>
        /// <returns>True if the key exists, otherwise false.</returns>
        Task<bool> ExistsAsync(string key);
    }
}
