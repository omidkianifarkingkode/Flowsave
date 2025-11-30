using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FlowSave.Logging
{
    /// <summary>
    /// Default FlowSave logger that wraps Unity's Debug API and
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

        [HideInCallstack]
        public bool IsLogTypeAllowed(LogType logType)
        {
            return Debug.unityLogger.IsLogTypeAllowed(logType);
        }

        [HideInCallstack]
        public void Log(LogType logType, object message)
        {
            LogInternal(logType, null, message, null);
        }

        [HideInCallstack]
        public void Log(LogType logType, object message, Object context)
        {
            LogInternal(logType, null, message, context);
        }

        [HideInCallstack]
        public void Log(LogType logType, string tag, object message)
        {
            LogInternal(logType, tag, message, null);
        }

        [HideInCallstack]
        public void Log(LogType logType, string tag, object message, Object context)
        {
            LogInternal(logType, tag, message, context);
        }

        [HideInCallstack]
        public void Log(object message)
        {
            LogInternal(LogType.Log, null, message, null);
        }

        [HideInCallstack]
        public void Log(string tag, object message)
        {
            LogInternal(LogType.Log, tag, message, null);
        }

        [HideInCallstack]
        public void Log(string tag, object message, Object context)
        {
            LogInternal(LogType.Log, tag, message, context);
        }

        [HideInCallstack]
        public void LogWarning(string tag, object message)
        {
            LogInternal(LogType.Warning, tag, message, null);
        }

        [HideInCallstack]
        public void LogWarning(string tag, object message, Object context)
        {
            LogInternal(LogType.Warning, tag, message, context);
        }

        [HideInCallstack]
        public void LogError(string tag, object message)
        {
            LogInternal(LogType.Error, tag, message, null);
        }

        [HideInCallstack]
        public void LogError(string tag, object message, Object context)
        {
            LogInternal(LogType.Error, tag, message, context);
        }

        [HideInCallstack]
        public void LogException(Exception exception)
        {
            Debug.unityLogger.LogException(exception);
        }

        [HideInCallstack]
        public void LogException(Exception exception, Object context)
        {
            Debug.unityLogger.LogException(exception, context);
        }

        [HideInCallstack]
        public void LogFormat(LogType logType, string format, params object[] args)
        {
            LogInternal(logType, null, string.Format(format, args), null);
        }

        [HideInCallstack]
        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            LogInternal(logType, null, string.Format(format, args), context);
        }

        #endregion

        #region High-level FlowSave logging API

        [HideInCallstack]
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

        #endregion

        #region Internals

        [HideInCallstack]
        private void LogInternal(LogType logType, string tag, object message, Object context)
        {
            if (!IsLogTypeAllowed(logType))
                return;

            // Map Unity log type → FlowSave log level for filtering
            var level = MapLogTypeToLevel(logType);
            if (!ShouldLog(level))
                return;

            var text = message?.ToString();
            if (!string.IsNullOrEmpty(tag))
                text = $"[{tag}] {text}";

            var formatted = FormatMessage(text);

            switch (logType)
            {
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    if (context != null) Debug.LogError(formatted, context);
                    else Debug.LogError(formatted);
                    break;

                case LogType.Warning:
                    if (context != null) Debug.LogWarning(formatted, context);
                    else Debug.LogWarning(formatted);
                    break;

                default:
                    if (context != null) Debug.Log(formatted, context);
                    else Debug.Log(formatted);
                    break;
            }
        }

        private static LogLevel MapLogTypeToLevel(LogType logType)
        {
            switch (logType)
            {
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    return LogLevel.Error;
                case LogType.Warning:
                    return LogLevel.Warning;
                default:
                    return LogLevel.Info;
            }
        }

        private bool ShouldLog(LogLevel level)
        {
            if (_options == null)
                return true;

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
