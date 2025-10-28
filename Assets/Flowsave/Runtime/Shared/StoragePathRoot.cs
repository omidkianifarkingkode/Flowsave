namespace Flowsave.Shared
{
    public enum StoragePathRoot
    {
        ProjectRoot,         // <Project>/
        PersistentDataPath,  // Application.persistentDataPath
        DataPath,            // Application.dataPath
        TemporaryCachePath,  // Application.temporaryCachePath
        Absolute             // treat 'path' as absolute
    }
}