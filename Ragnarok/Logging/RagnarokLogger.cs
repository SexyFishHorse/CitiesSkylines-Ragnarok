namespace SexyFishHorse.CitiesSkylines.Ragnarok.Logging
{
    using System;
    using ColossalFramework.Plugins;
    using Configuration;
    using JetBrains.Annotations;
    using Logger;

    public class RagnarokLogger : ILogger
    {
        private static ILogger internalLogger;

        private ILogger logger;

        private RagnarokLogger()
        {
            logger = LogManager.Instance.GetOrCreateLogger(RagnarokUserMod.ModName);
        }

        public static ILogger Instance
        {
            get
            {
                return internalLogger ?? (internalLogger = new RagnarokLogger());
            }
        }

        public void Dispose()
        {
            logger.Dispose();
        }

        [StringFormatMethod("message")]
        public void Error(string message, params object[] args)
        {
            logger.Error(message, args);
        }

        public void Info(string message, params object[] args)
        {
            if (IsDisabled())
            {
                return;
            }

            logger.Info(message, args);
        }

        [StringFormatMethod("message")]
        public void Log(PluginManager.MessageType messageType, string message, params object[] args)
        {
            if (IsDisabled() && (messageType != PluginManager.MessageType.Error))
            {
                return;
            }

            logger.Log(messageType, message, args);
        }

        public void LogException(Exception exception)
        {
            logger.LogException(exception);
        }

        [StringFormatMethod("message")]
        public void Warn(string message, params object[] args)
        {
            if (IsDisabled())
            {
                return;
            }

            logger.Warn(message, args);
        }

        private bool IsDisabled()
        {
            return !ModConfig.Instance.GetSetting<bool>(SettingKeys.EnableLogging);
        }
    }
}
