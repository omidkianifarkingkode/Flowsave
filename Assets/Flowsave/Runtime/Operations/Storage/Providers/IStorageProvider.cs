using System.Threading.Tasks;

namespace FlowSave.Storage
{
    /// <summary>
    /// Defines a contract for data storage operations (asynchronous).
    /// </summary>
    public interface IStorageProvider
    {
        /// <summary>
        /// Persists a binary payload associated with the provided key.
        /// </summary>
        Task<Result> SaveAsync(string key, byte[] data);

        /// <summary>
        /// Loads a previously persisted payload.
        /// </summary>
        Task<Result<byte[]>> LoadAsync(string key);

        /// <summary>
        /// Removes a persisted payload associated with the provided key.
        /// </summary>
        Task<Result> DeleteAsync(string key);

        /// <summary>
        /// Checks whether the provided key exists in the underlying storage.
        /// </summary>
        Task<Result<bool>> ExistsAsync(string key);
    }
}
