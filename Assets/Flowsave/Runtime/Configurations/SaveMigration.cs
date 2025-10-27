namespace Flowsave.Configurations
{
    [System.Serializable]
    public class SaveMigration
    {
        public int fromVersion;
        public int toVersion;
        public string migrationScript;
    }

}