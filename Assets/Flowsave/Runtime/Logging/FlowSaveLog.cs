using UnityEngine;

namespace FlowSave.Logging
{
    public static class FlowSaveLog
    {
        private static LoggingOptions _options = new();
        private static readonly UnityLogger DefaultLogger = new(_options);
        private static ILogger _logger = DefaultLogger;

        public static ILogger Logger
        {
            get => _logger;
            set => _logger = value ?? DefaultLogger;
        }

        public static void SetLogger(ILogger logger)
        {
            _logger = logger ?? DefaultLogger;
        }

        [HideInCallstack]
        public static void Log(string message, LogLevel level = LogLevel.Info) => _logger.Log(message, level);
        [HideInCallstack]
        public static void Debug(string message) => Log(message, LogLevel.Debug);
        [HideInCallstack]
        public static void Info(string message) => Log(message, LogLevel.Info);
        [HideInCallstack]
        public static void Warning(string message) => Log(message, LogLevel.Warning);
        [HideInCallstack]
        public static void Error(string message) => Log(message, LogLevel.Error);
    }
}
