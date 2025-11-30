using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowSave.Storage
{
    /// <summary>
    /// Async, disk-backed storage provider with atomic writes (unless Append=true).
    /// Supports backup retention and configurable path templates.
    /// </summary>
    public sealed class DiskStorageProvider : IStorageProvider
    {
        private readonly string _root;
        private readonly string _pathTemplate;

        private readonly bool _append;
        private readonly bool _keepBackup;
        private readonly int _maxBackup;

        public DiskStorageProvider(DiskStorageOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _root = PathResolver.Resolve(options.PathRoot, null);
            _pathTemplate = options.PathTemplate ?? "saves/{NAMESPACE}.json";

            _append = options.Append;
            _keepBackup = options.KeepBackup;
            _maxBackup = Math.Max(1, options.MaxBackup);
        }

        // ============================================================
        // SAVE
        // ============================================================

        public async Task<Result> SaveAsync(string key, byte[] data)
        {
            if (string.IsNullOrWhiteSpace(key))
                return StorageErrors.KeyRequiredResult;
            if (data == null)
                return StorageErrors.DataNull;

            try
            {
                var path = GetPath(key);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (_append)
                {
                    return await AppendWriteAsync(path, data).ConfigureAwait(false);
                }
                else
                {
                    return await AtomicWriteAsync(path, data).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                return StorageErrors.SaveFailed(ex.Message, StorageErrors.DiskModule);
            }
        }


        // ============================================================
        // ATOMIC WRITE MODE (default)
        // ============================================================

        private async Task<Result> AtomicWriteAsync(string path, byte[] data)
        {
            var tmp = path + ".tmp";

            using (var fs = new FileStream(tmp,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                64 * 1024,
                useAsync: true))
            {
                await fs.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                await fs.FlushAsync().ConfigureAwait(false);
            }

            // Backup rotation
            if (_keepBackup && File.Exists(path))
                RotateBackups(path);

            try
            {
                // Try atomic replace
                File.Replace(tmp, path, null, ignoreMetadataErrors: true);
                return Result.Success();
            }
            catch
            {
                // Fallback to delete + move
                try { if (File.Exists(path)) File.Delete(path); } catch { }
            }

            File.Move(tmp, path);
            return Result.Success();
        }

        // ============================================================
        // APPEND MODE
        // ============================================================

        private async Task<Result> AppendWriteAsync(string path, byte[] data)
        {
            // Create directory if needed
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Optional backup
            if (_keepBackup && File.Exists(path))
                RotateBackups(path);

            using var fs = new FileStream(path,
                FileMode.Append,
                FileAccess.Write,
                FileShare.None,
                64 * 1024,
                useAsync: true);

            await fs.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
            await fs.FlushAsync().ConfigureAwait(false);

            return Result.Success();
        }

        // ============================================================
        // BACKUP ROTATION
        // ============================================================

        private void RotateBackups(string path)
        {
            // bak.1 ... bak.N (N oldest)
            for (int i = _maxBackup - 1; i >= 1; i--)
            {
                string older = $"{path}.bak.{i}";
                string newer = $"{path}.bak.{i + 1}";

                if (File.Exists(newer)) File.Delete(newer);
                if (File.Exists(older)) File.Move(older, newer);
            }

            // Move main file → bak.1
            string bak1 = $"{path}.bak.1";

            if (File.Exists(bak1))
                File.Delete(bak1);

            File.Copy(path, bak1, overwrite: true);
        }

        // ============================================================
        // LOAD
        // ============================================================

        public async Task<Result<byte[]>> LoadAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return StorageErrors.KeyRequired;

            try
            {
                var path = GetPath(key);
                if (!File.Exists(path))
                    return StorageErrors.KeyNotFound(key, StorageErrors.DiskModule);

                using var fs = new FileStream(path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    64 * 1024,
                    useAsync: true);

                var len = fs.Length;
                if (len > int.MaxValue)
                    return StorageErrors.FileTooLarge(len);

                var buffer = new byte[len];

                int offset = 0;
                while (true)
                {
                    int read = await fs.ReadAsync(buffer, offset, buffer.Length - offset).ConfigureAwait(false);
                    if (read == 0) break;
                    offset += read;
                }

                return Result<byte[]>.Success(buffer);
            }
            catch (Exception ex)
            {
                return StorageErrors.LoadFailed(ex.Message, StorageErrors.DiskModule);
            }
        }

        // ============================================================
        // DELETE
        // ============================================================

        public Task<Result> DeleteAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return StorageErrors.KeyRequiredResult.ToTask();

            try
            {
                var path = GetPath(key);

                if (File.Exists(path))
                    File.Delete(path);

                if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");

                for (int i = 1; i <= _maxBackup; i++)
                {
                    var bak = $"{path}.bak.{i}";
                    if (File.Exists(bak)) File.Delete(bak);
                }

                return Result.Success().ToTask();
            }
            catch (Exception ex)
            {
                return StorageErrors.DeleteFailed(ex.Message, StorageErrors.DiskModule).ToTask();
            }
        }

        // ============================================================
        // EXISTS
        // ============================================================

        public Task<Result<bool>> ExistsAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return StorageErrors.KeyRequiredBool.ToTask();

            try
            {
                var path = GetPath(key);
                return Result<bool>.Success(File.Exists(path)).ToTask();
            }
            catch (Exception ex)
            {
                return StorageErrors.ExistsFailed(ex.Message, StorageErrors.DiskModule).ToTask();
            }
        }

        // ============================================================
        // PATH + SANITIZATION
        // ============================================================

        private string GetPath(string key)
        {
            var safe = SanitizeKey(key);
            var local = _pathTemplate.Replace("{NAMESPACE}", safe);
            return Path.Combine(_root, local);
        }

        private static string SanitizeKey(string key)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(key.Length);

            foreach (var ch in key)
                sb.Append(invalid.Contains(ch) ? '_' : ch);

            return sb.ToString();
        }
    }
}
