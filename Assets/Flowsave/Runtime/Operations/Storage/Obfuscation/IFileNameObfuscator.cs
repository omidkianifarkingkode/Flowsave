namespace FlowSave.Storage
{
    public interface IFileNameObfuscator
    {
        string ObfuscateFilename(string filename);
    }
}
