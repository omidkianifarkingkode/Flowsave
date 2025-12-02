using FlowSave.Configurations;
using FlowSave.KeyStorage;
using System;

namespace FlowSave.KeyStorage
{
    [Serializable]
    public class AppModeKeyStore
    {
        public AppMode AppMode;
        public KeyStoreOptions KeyStore = new();
    }
}
