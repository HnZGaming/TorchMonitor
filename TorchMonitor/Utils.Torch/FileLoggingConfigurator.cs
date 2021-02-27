using NLog;
using NLog.Config;
using NLog.Targets;

namespace Utils.Torch
{
    internal class FileLoggingConfigurator
    {
        public interface IConfig
        {
            bool SuppressWpfOutput { get; }
            bool EnableLoggingTrace { get; }
            bool EnableLoggingDebug { get; }
            string LogFilePath { get; }
        }

        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        readonly FileTarget _target;
        readonly LoggingRule[] _rules;

        public FileLoggingConfigurator(string targetName, string[] namePatterns, string defaultFilePath)
        {
            _target = new FileTarget
            {
                Name = targetName,
            };

            _target.FileName = defaultFilePath;

            _rules = new LoggingRule[namePatterns.Length];
            for (var i = 0; i < namePatterns.Length; i++)
            {
                var rule = new LoggingRule
                {
                    LoggerNamePattern = namePatterns[i],
                    Final = true,
                };

                rule.Targets.Add(_target);
                rule.Targets.Add(TorchUtils.GetWpfTarget());
                rule.EnableLoggingForLevels(LogLevel.Info, LogLevel.Off);
                _rules[i] = rule;
            }
        }

        public void Initialize()
        {
            LogManager.Configuration.AddTarget(_target);

            foreach (var rule in _rules)
            {
                LogManager.Configuration.LoggingRules.Insert(0, rule);
            }

            LogManager.Configuration.Reload();
        }

        public void Configure(IConfig config)
        {
            _target.FileName = config.LogFilePath;

            foreach (var rule in _rules)
            {
                rule.Targets.Clear();
                rule.Targets.Add(_target);

                if (!config.SuppressWpfOutput)
                {
                    rule.Targets.Add(TorchUtils.GetWpfTarget());
                }

                var minLevel = GetMinLogLevel(config);
                rule.DisableLoggingForLevel(LogLevel.Trace);
                rule.DisableLoggingForLevel(LogLevel.Debug);
                rule.EnableLoggingForLevels(minLevel, LogLevel.Off);

                Log.Info($"Reconfigured; pattern={rule.LoggerNamePattern}, wpf={!config.SuppressWpfOutput}, minlevel={minLevel}");
            }

            LogManager.ReconfigExistingLoggers();
        }

        LogLevel GetMinLogLevel(IConfig config)
        {
            if (config.EnableLoggingTrace) return LogLevel.Trace;
            if (config.EnableLoggingDebug) return LogLevel.Debug;
            return LogLevel.Info;
        }
    }
}