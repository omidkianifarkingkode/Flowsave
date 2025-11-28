#if FLOWSAVE_UNITASK
using Cysharp.Threading.Tasks;

namespace Flowsave
{
    public sealed partial class FlowSave
    {
        // ============================================================
        //  High-level (with serialization) – UniTask wrappers
        // ============================================================

        public async UniTask<Result> SaveUniAsync<T>(string namespaceId, T data) =>
            await SaveAsync<T>(namespaceId, data);

        public async UniTask<Result<T>> LoadUniAsync<T>(string namespaceId) =>
            await LoadAsync<T>(namespaceId);

        public async UniTask<Result<bool>> HasSaveUniAsync(string namespaceId) =>
            await HasSaveAsync(namespaceId);

        public async UniTask<Result> DeleteSaveUniAsync(string namespaceId) =>
            await DeleteSaveAsync(namespaceId);

        // ============================================================
        //  Raw (no serialization) – UniTask wrappers
        // ============================================================

        public async UniTask<Result> SaveRawBytesUniAsync(string namespaceId, byte[] data) =>
            await SaveRawBytesAsync(namespaceId, data);

        public async UniTask<Result<byte[]>> LoadRawBytesUniAsync(string namespaceId) =>
            await LoadRawBytesAsync(namespaceId);

        public async UniTask<Result> SaveRawStringUniAsync(string namespaceId, string text) =>
            await SaveRawStringAsync(namespaceId, text);

        public async UniTask<Result<string>> LoadRawStringUniAsync(string namespaceId) =>
            await LoadRawStringAsync(namespaceId);

    }
}

#endif
