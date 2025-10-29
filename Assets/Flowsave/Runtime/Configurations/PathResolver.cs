// File: Flowsave/Configurations/PathResolver.cs
using Flowsave.Shared;
using System.IO;
using UnityEngine;

namespace Flowsave.Configurations
{
    public static class PathResolver
    {
        public static string Resolve(StoragePathRoot root, string subPath)
        {
            string basePath = root switch
            {
                StoragePathRoot.ProjectRoot => Directory.GetParent(Application.dataPath)?.FullName ?? "",
                StoragePathRoot.PersistentDataPath => Application.persistentDataPath,
                StoragePathRoot.DataPath => Application.dataPath,
                StoragePathRoot.TemporaryCachePath => Application.temporaryCachePath,
                StoragePathRoot.Absolute => "",
                _ => ""
            };

            if (root == StoragePathRoot.Absolute)
                return subPath ?? "";

            if (string.IsNullOrEmpty(basePath))
                return subPath ?? "";

            if (string.IsNullOrEmpty(subPath))
                return basePath;

            return Path.GetFullPath(Path.Combine(basePath, subPath));
        }
    }
}
