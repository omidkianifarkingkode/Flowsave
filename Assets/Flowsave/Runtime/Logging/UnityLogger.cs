using UnityEngine;

namespace FlowSave.Logging
{
    public interface ILogger
    {
        void Log(string message, LogLevel level = LogLevel.Info);
    }

    public class UnityLogger : ILogger
    {
        private LoggingOptions _options;

        public UnityLogger(LoggingOptions options = null)
        {
            _options = LoggingOptions.Clone(options);
        }

        public LoggingOptions Options => _options;

        public void SetOptions(LoggingOptions options)
        {
            _options = LoggingOptions.Clone(options);
        }

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
            return _options.MinimumLevel != LogLevel.None && level <= _options.MinimumLevel;
        }

        private string FormatMessage(string message)
        {
            string cleanMessage = CleanMessage(message);
            string prefix = CleanMessage(_options.Prefix);

            if (string.IsNullOrEmpty(prefix))
                return cleanMessage;

            string formattedPrefix = prefix;

#if UNITY_EDITOR
            if (_options.UseColorInEditor && Application.isEditor)
            {
                string hex = ColorUtility.ToHtmlStringRGB(_options.EditorColor);
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
    }
}
