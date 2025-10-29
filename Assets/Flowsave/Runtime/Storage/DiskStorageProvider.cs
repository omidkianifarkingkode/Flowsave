using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowsave.StorageProviders
{
    /// <summary>
    /// Async, disk-backed storage provider with best-effort atomic writes.
    /// </summary>
    public sealed class DiskStorageProvider : IStorageProvider
    {
        private readonly string _root;
        private readonly string _extension;
        private readonly bool _keepBackup;

        /// <param name="rootDirectory">Base folder where records are stored.</param>
        /// <param name="fileExtension">Optional file extension (e.g., ".dat").</param>
        /// <param name="keepBackup">If true, keeps a .bak copy of the last version when replacing.</param>
        public DiskStorageProvider(string rootDirectory,
                                   string fileExtension = ".dat",
                                   bool keepBackup = true)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
                throw new ArgumentException("Root directory is required.", nameof(rootDirectory));

            _root = rootDirectory;
            _extension = string.IsNullOrEmpty(fileExtension) ? ".dat" : (fileExtension.StartsWith(".") ? fileExtension : "." + fileExtension);
            _keepBackup = keepBackup;
        }

        public async Task SaveAsync(string key, byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var path = GetPath(key);
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir!);

            var tmp = path + ".tmp";

            // Write temp file asynchronously
            // useAsync:true enables true async I/O on supported platforms.
            using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None,
                                           bufferSize: 64 * 1024, useAsync: true))
            {
                await fs.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
                await fs.FlushAsync().ConfigureAwait(false);
            }

            // If destination exists, optionally keep a backup and atomically replace where supported.
            if (File.Exists(path))
            {
                if (_keepBackup)
                {
                    var bak = path + ".bak";
                    try { File.Copy(path, bak, overwrite: true); } catch { /* best-effort */ }
                }

                // On Windows/.NET, File.Replace is an atomic replace into existing file.
                try
                {
                    // If File.Replace throws (e.g., not supported), fall back to delete+move.
                    File.Replace(tmp, path, destinationBackupFileName: null, ignoreMetadataErrors: true);
                    return;
                }
                catch
                {
                    try { File.Delete(path); } catch { /* best-effort */ }
                }
            }

            // Move tmp into place (first write / fallback path)
            File.Move(tmp, path);
        }

        public async Task<byte[]> LoadAsync(string key)
        {
            var path = GetPath(key);
            if (!File.Exists(path))
                throw new InvalidOperationException($"Key not found: {key}");

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                                           bufferSize: 64 * 1024, useAsync: true))
            {
                var len = fs.Length;
                if (len > int.MaxValue) throw new IOException("File too large to load into memory.");
                var buffer = new byte[len];
                int read, offset = 0;
                while ((read = await fs.ReadAsync(buffer, offset, buffer.Length - offset).ConfigureAwait(false)) > 0)
                    offset += read;
                return buffer;
            }
        }

        public Task DeleteAsync(string key)
        {
            var path = GetPath(key);
            if (File.Exists(path))
                File.Delete(path);

            // Clean up temp/backup if present
            var tmp = path + ".tmp"; if (File.Exists(tmp)) File.Delete(tmp);
            var bak = path + ".bak"; if (File.Exists(bak)) File.Delete(bak);

            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key)
        {
            var path = GetPath(key);
            return Task.FromResult(File.Exists(path));
        }

        private string GetPath(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key is required.", nameof(key));

            // Prevent directory traversal and invalid characters
            var safe = SanitizeKey(key);
            return Path.Combine(_root, safe + _extension);
        }

        private static string SanitizeKey(string key)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(key.Length);
            foreach (var ch in key)
                sb.Append(invalid.Contains(ch) || ch == Path.DirectorySeparatorChar || ch == Path.AltDirectorySeparatorChar ? '_' : ch);
            return sb.ToString();
        }
    }
}
