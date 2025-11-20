using System;
using UnityEngine;

namespace Flowsave.Compression
{
    [Serializable]
    public class DefaultCompressionOptions
    {
        public CompressionType CompressionType = CompressionType.None;
    }

    [Serializable]
    public class CompressionOptions : DefaultCompressionOptions
    {
        public bool UseDefault = true;

        public static CompressionOptions Clone(DefaultCompressionOptions from) =>
            from == null ? null : new CompressionOptions
            {
                UseDefault = true,
                CompressionType = from.CompressionType
            };


        public static CompressionOptions Clone(CompressionOptions from) =>
            from == null ? null : new CompressionOptions
            {
                UseDefault = from.UseDefault,
                CompressionType = from.CompressionType
            };

    }
}
