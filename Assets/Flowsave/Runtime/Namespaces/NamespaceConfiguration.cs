using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Flowsave.Namespaces
{
    [CreateAssetMenu(fileName ="Namespace")]
    public class NamespaceConfiguration : ScriptableObject
    {
        [field: SerializeField] public string NamespaceId { get; private set; } = "[namespace]";
        [SerializeField] List<NamespaceProperties> environmentsProperties = new();

        [SerializeField] private bool overrideDevelopment;
        [SerializeField] private bool overrideRelease;

        public NamespaceProperties GetProperties(EnvironmentMode mode)
        {
            var properties = environmentsProperties.FirstOrDefault(e => e.Environment == mode);
            
            return properties ??
                throw new InvalidOperationException($"No properties-data found for environment-mode {mode} in namespace {NamespaceId}");
        }
    }
}
