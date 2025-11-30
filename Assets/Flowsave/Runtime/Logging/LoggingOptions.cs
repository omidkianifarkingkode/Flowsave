using System;
using UnityEngine;

namespace FlowSave.Logging
{
    public enum LogLevel
    {
        None = 0,
        Error = 1,
        Warning = 2,
        Info = 3,
        Debug = 4
    }

    [Serializable]
    public class LoggingOptions
    {
        public LogLevel MinimumLevel = LogLevel.Info;
        public string Prefix = "[FlowSave]";
        public bool UseColorInEditor = true;
        public Color EditorColor = new(0.29f, 0.6f, 1f);

        public static LoggingOptions Clone(LoggingOptions from) =>
            from == null ? new LoggingOptions() : new LoggingOptions
            {
                MinimumLevel = from.MinimumLevel,
                Prefix = from.Prefix,
                UseColorInEditor = from.UseColorInEditor,
                EditorColor = from.EditorColor
            };
    }
}
