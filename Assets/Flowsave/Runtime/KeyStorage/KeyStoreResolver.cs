using FlowSave.Configurations;
using FlowSave.KeyStorage;
using System;
using System.Collections.Generic;

namespace Flowsave.KeyStorage
{
    public sealed class KeyStoreResolver
    {
        private readonly IReadOnlyList<AppModeKeyStore> _stores;

        public KeyStoreResolver(IReadOnlyList<AppModeKeyStore> stores)
        {
            _stores = stores ?? Array.Empty<AppModeKeyStore>();
        }

        public KeyStoreOptions Resolve(AppMode effectiveMode)
        {
            if (effectiveMode == AppMode.None || _stores.Count == 0)
                return null;

            int mask = (int)effectiveMode;

            AppModeKeyStore best = null;

            foreach (var s in _stores)
            {
                if (s == null || s.KeyStore == null)
                    continue;

                int sMask = (int)s.AppMode;
                if ((sMask & mask) == 0)
                    continue; // no overlap

                // simple rule: prefer exact match; otherwise first overlapping
                if (sMask == mask)
                    return s.KeyStore;

                if (best == null)
                    best = s;
            }

            return best?.KeyStore;
        }
    }

}
