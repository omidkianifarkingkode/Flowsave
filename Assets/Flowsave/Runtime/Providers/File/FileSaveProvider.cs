using System;
using System.IO;
using System.Text;

namespace FlowSave
{
    /// <summary>
    /// Persists save data to disk using the local filesystem.
    /// </summary>
    public class FileSaveProvider : ISaveProvider
    {
        private readonly string _rootDirectory;

        public FileSaveProvider(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException("Root directory cannot be null or empty.", nameof(rootDirectory));
            }

            _rootDirectory = Path.GetFullPath(rootDirectory);
            Directory.CreateDirectory(_rootDirectory);
        }

        public void Save(string key, byte[] data)
        {
            string filePath = ResolveFilePath(key);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllBytes(filePath, data ?? Array.Empty<byte>());
        }

        public byte[] Load(string key)
        {
            string filePath = ResolveFilePath(key);
            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException($"Save key '{key}' does not exist.");
            }

            return File.ReadAllBytes(filePath);
        }

        public void Delete(string key)
        {
            string filePath = ResolveFilePath(key);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public bool Exists(string key)
        {
            string filePath = ResolveFilePath(key);
            return File.Exists(filePath);
        }

        private string ResolveFilePath(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));
            }

            string sanitized = SanitizeKey(key);
            return Path.Combine(_rootDirectory, sanitized + ".dat");
        }

        private static string SanitizeKey(string key)
        {
            var builder = new StringBuilder(key.Length);
            char[] invalidChars = Path.GetInvalidFileNameChars();

            foreach (char c in key)
            {
                if (Array.IndexOf(invalidChars, c) >= 0 || c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar)
                {
                    builder.Append('_');
                }
                else
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }
    }
}
