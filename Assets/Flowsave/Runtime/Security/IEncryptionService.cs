namespace FlowSave.Security
{
    public interface IEncryptionService
    {
        byte[] Encrypt(byte[] data, string key, string iv);
        byte[] Decrypt(byte[] data, string key, string iv);
    }

    public interface IHashingService
    {
        byte[] ComputeHash(byte[] data, string key);
    }

    public interface IFileObfuscationService
    {
        string ObfuscateFilename(string filename);
    }

    public class HashingService : IHashingService
    {
        public byte[] ComputeHash(byte[] data, string key)
        {
            return SecurityHelper.ComputeHmacSha256(data, key);
        }
    }

    public class FileObfuscationService : IFileObfuscationService
    {
        public string ObfuscateFilename(string filename)
        {
            return SecurityHelper.ObfuscateFilename(filename);
        }
    }

    public class EncryptionService : IEncryptionService
    {
        public byte[] Encrypt(byte[] data, string key, string iv)
        {
            return SecurityHelper.Encrypt(data, key, iv);
        }

        public byte[] Decrypt(byte[] data, string key, string iv)
        {
            return SecurityHelper.Decrypt(data, key, iv);
        }
    }
}
