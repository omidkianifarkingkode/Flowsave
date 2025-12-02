using System.Threading.Tasks;
#if FLOWSAVE_UNITASK
using Cysharp.Threading.Tasks;
#endif

namespace FlowSave
{
    public interface IFlowSave
    {
        // ─────────────────────────────────────────────
        //  High-level (with serialization) – Task
        // ─────────────────────────────────────────────

        /// <summary>Saves a typed object using the configured serializer + pipeline.</summary>
        Task<Result> SaveAsync<T>(string namespaceId, T data);

        /// <summary>Loads a typed object using the configured serializer + pipeline.</summary>
        Task<Result<T>> LoadAsync<T>(string namespaceId);


        Task<Result<T[]>> LoadAllAsync<T>(string namespaceId);

        /// <summary>Checks if a save exists for the given namespace.</summary>
        Task<Result<bool>> HasSaveAsync(string namespaceId);

        /// <summary>Deletes a save for the given namespace.</summary>
        Task<Result> DeleteSaveAsync(string namespaceId);

        // ─────────────────────────────────────────────
        //  Raw (no serialization) – Task
        // ─────────────────────────────────────────────

        /// <summary>
        /// Saves raw bytes through the compression/encryption/signing/storage pipeline,
        /// but skips serialization (SerializationType.None).
        /// </summary>
        Task<Result> SaveRawBytesAsync(string namespaceId, byte[] data);

        /// <summary>
        /// Loads raw bytes through the reverse pipeline (verify/decrypt/decompress),
        /// but skips deserialization.
        /// </summary>
        Task<Result<byte[]>> LoadRawBytesAsync(string namespaceId);

        /// <summary>
        /// Saves a raw string as UTF8 bytes (no serialization step).
        /// </summary>
        Task<Result> SaveRawStringAsync(string namespaceId, string text);

        /// <summary>
        /// Loads raw bytes and decodes them as UTF8 string (no deserialization).
        /// </summary>
        Task<Result<string>> LoadRawStringAsync(string namespaceId);

        #if FLOWSAVE_UNITASK

        // ─────────────────────────────────────────────
        //  High-level (with serialization) – UniTask
        // ─────────────────────────────────────────────

        UniTask<Result> SaveUniAsync<T>(string namespaceId, T data);
        UniTask<Result<T>> LoadUniAsync<T>(string namespaceId);
        UniTask<Result<bool>> HasSaveUniAsync(string namespaceId);
        UniTask<Result> DeleteSaveUniAsync(string namespaceId);

        // ─────────────────────────────────────────────
        //  Raw (no serialization) – UniTask
        // ─────────────────────────────────────────────

        UniTask<Result> SaveRawBytesUniAsync(string namespaceId, byte[] data);
        UniTask<Result<byte[]>> LoadRawBytesUniAsync(string namespaceId);
        UniTask<Result> SaveRawStringUniAsync(string namespaceId, string text);
        UniTask<Result<string>> LoadRawStringUniAsync(string namespaceId);

        #endif
    }
}
