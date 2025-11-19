using UnityEngine;

namespace Flowsave.Compression
{
    public class CompressionOptions
    {
        [field: SerializeField] public CompressionType CompressionType { get; private set; } = CompressionType.None;
        [field: SerializeField] public bool UseDefault { get; private set; } = true;
    }
}
