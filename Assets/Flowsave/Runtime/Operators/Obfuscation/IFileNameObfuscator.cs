namespace Flowsave.Security
{
    public interface IFileNameObfuscator
    {
        string ObfuscateFilename(string filename);
    }
}
