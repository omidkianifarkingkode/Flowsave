using Flowsave.Shared;

namespace Flowsave.Configurations
{
    [System.Serializable]
    public class EncryptionSettings
    {
        public EncryptionType encryptionType;
        public string key;
        public string iv;
    }

}