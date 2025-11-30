using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FlowSave.Logging
{
    /// <summary>
    /// Default FlowSave logger that wraps Unity's Debug.unityLogger and
    /// adds prefix / color formatting based on LoggingOptions.
    /// </summary>
    [Serializable]
    public class UnityLogger : ILogger
    {
        private LoggingOptions _options;

        public UnityLogger(LoggingOptions options)
        {
            _options = LoggingOptions.Clone(options) ?? new LoggingOptions();
        }

        public LoggingOptions Options => _options;

        #region UnityEngine.ILogger implementation (backed by Debug.unityLogger)

        public ILogHandler logHandler
        {
            get => Debug.unityLogger.logHandler;
            set => Debug.unityLogger.logHandler = value;
        }

        public bool logEnabled
        {
            get => Debug.unityLogger.logEnabled;
            set => Debug.unityLogger.logEnabled = value;
        }

        public LogType filterLogType
        {
            get => Debug.unityLogger.filterLogType;
            set => Debug.unityLogger.filterLogType = value;
        }

        public bool IsLogTypeAllowed(LogType logType)
        {
            return Debug.unityLogger.IsLogTypeAllowed(logType);
        }

        public void Log(LogType logType, object message)
        {
            if (!IsLogTypeAllowed(logType)) return;
            Debug.unityLogger.Log(logType, FormatMessage(message?.ToString()));
        }

        public void Log(LogType logType, object message, Object context)
        {
            if (!IsLogTypeAllowed(logType)) return;

            // Explicit cast so the compiler picks (LogType, object, Object)
            Debug.unityLogger.Log(
                logType,
                (object)FormatMessage(message?.ToString()),
                context
            );
        }

        public void Log(LogType logType, string tag, object message)
        {
            if (!IsLogTypeAllowed(logType)) return;
            Debug.unityLogger.Log(logType, $"[{tag}] {FormatMessage(message?.ToString())}");
        }

        public void Log(LogType logType, string tag, object message, Object context)
        {
            if (!IsLogTypeAllowed(logType)) return;

            // Use the (LogType, string tag, object message, Object context) overload explicitly
            Debug.unityLogger.Log(
                logType,
                tag,
                (object)FormatMessage(message?.ToString()),
                context
            );
        }

        public void Log(object message)
        {
            Debug.unityLogger.Log(FormatMessage(message?.ToString()));
        }

        public void Log(string tag, object message)
        {
            Debug.unityLogger.Log($"[{tag}] {FormatMessage(message?.ToString())}");
        }

        public void Log(string tag, object message, Object context)
        {
            Debug.unityLogger.Log($"[{tag}] {FormatMessage(message?.ToString())}", context);
        }

        public void LogWarning(string tag, object message)
        {
            Debug.unityLogger.LogWarning(tag, FormatMessage(message?.ToString()));
        }

        public void LogWarning(string tag, object message, Object context)
        {
            Debug.unityLogger.LogWarning(tag, FormatMessage(message?.ToString()), context);
        }

        public void LogError(string tag, object message)
        {
            Debug.unityLogger.LogError(tag, FormatMessage(message?.ToString()));
        }

        public void LogError(string tag, object message, Object context)
        {
            Debug.unityLogger.LogError(tag, FormatMessage(message?.ToString()), context);
        }

        public void LogException(Exception exception)
        {
            Debug.unityLogger.LogException(exception);
        }

        public void LogException(Exception exception, Object context)
        {
            Debug.unityLogger.LogException(exception, context);
        }

        public void LogFormat(LogType logType, string format, params object[] args)
        {
            if (!IsLogTypeAllowed(logType)) return;
            Debug.unityLogger.LogFormat(logType, FormatMessage(string.Format(format, args)));
        }

        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            if (!IsLogTypeAllowed(logType)) return;
            Debug.unityLogger.LogFormat(logType, context, FormatMessage(string.Format(format, args)));
        }

        #endregion

        #region High-level FlowSave logging API

        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            if (!ShouldLog(level))
                return;

            var formatted = FormatMessage(message);

            switch (level)
            {
                case LogLevel.Error:
                    Debug.LogError(formatted);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(formatted);
                    break;
                default:
                    Debug.Log(formatted);
                    break;
            }
        }

        private bool ShouldLog(LogLevel level)
        {
            if (_options == null)
                return true; // be safe: log if options somehow missing

            if (_options.MinimumLevel == LogLevel.None)
                return false;

            return level <= _options.MinimumLevel;
        }

        private string FormatMessage(string message)
        {
            var opts = _options ?? new LoggingOptions();

            string cleanMessage = CleanMessage(message);
            string prefix = CleanMessage(opts.Prefix);

            if (string.IsNullOrEmpty(prefix))
                return cleanMessage;

            string formattedPrefix = prefix;

#if UNITY_EDITOR
            if (opts.UseColorInEditor && Application.isEditor)
            {
                string hex = ColorUtility.ToHtmlStringRGB(opts.EditorColor);
                formattedPrefix = $"<color=#{hex}>{prefix}</color>";
            }
#endif

            if (string.IsNullOrEmpty(cleanMessage))
                return formattedPrefix;

            return $"{formattedPrefix} {cleanMessage}";
        }

        private static string CleanMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return string.Empty;

            string cleaned = message.Trim();

            while (cleaned.Contains("  "))
            {
                cleaned = cleaned.Replace("  ", " ");
            }

            return cleaned.Replace("\r\n", "\n");
        }

        #endregion
    }
}
