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

        public static LoggingOptions Options => LoggingOptions.Clone(_options);

        public static void Configure(LoggingOptions options, ILogger logger = null)
        {
            _options = LoggingOptions.Clone(options);
            DefaultLogger.SetOptions(_options);

            if (logger != null)
                _logger = logger;
            else if (_logger == null)
                _logger = DefaultLogger;

            if (_logger is UnityLogger unityLogger)
                unityLogger.SetOptions(_options);
        }

        public static void Log(string message, LogLevel level = LogLevel.Info) =>
            (_logger ?? DefaultLogger).Log(message, level);

        public static void Debug(string message) => Log(message, LogLevel.Debug);
        public static void Info(string message) => Log(message, LogLevel.Info);
        public static void Warning(string message) => Log(message, LogLevel.Warning);
        public static void Error(string message) => Log(message, LogLevel.Error);
    }
}
