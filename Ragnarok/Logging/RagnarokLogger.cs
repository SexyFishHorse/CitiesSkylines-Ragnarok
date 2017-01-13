﻿namespace SexyFishHorse.CitiesSkylines.Ragnarok.Logging
{
    using System;
    using ColossalFramework.Plugins;
    using JetBrains.Annotations;
    using Logger;

    public class RagnarokLogger : ILogger
    {
        private static ILogger internalLogger;

        private ILogger logger;

        private RagnarokLogger()
        {
            try
            {
                logger = LogManager.Instance.GetOrCreateLogger(RagnarokUserMod.ModName);
            }
            catch (Exception ex)
            {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Error, "[Ragnarok][RagnarokLogger]: " + ex.Message);
            }
        }

        public static bool Enabled { get; set; }

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
            try
            {
                logger.Error(message, args);
            }
            catch (Exception ex)
            {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Error, "[Ragnarok][RagnarokLogger.Error] ERROR: " + ex.Message);
            }
        }

        public void Info(string message, params object[] args)
        {
            if (!Enabled)
            {
                return;
            }

            try
            {
                logger.Info(message, args);
            }
            catch (Exception ex)
            {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Error, "[Ragnarok][RagnarokLogger.Info] ERROR: " + ex.Message);
            }
        }

        [StringFormatMethod("message")]
        public void Log(PluginManager.MessageType messageType, string message, params object[] args)
        {
            if (!Enabled && (messageType != PluginManager.MessageType.Error))
            {
                return;
            }

            try
            {
                logger.Log(messageType, message, args);
            }
            catch (Exception ex)
            {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Error, "[Ragnarok][RagnarokLogger.Log] ERROR: " + ex.Message);
            }
        }

        public void LogException(Exception exception)
        {
            try
            {
                logger.LogException(exception);
            }
            catch (Exception ex)
            {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Error, "[Ragnarok][RagnarokLogger.LogException] ERROR: " + ex.Message);
            }
        }

        [StringFormatMethod("message")]
        public void Warn(string message, params object[] args)
        {
            if (!Enabled)
            {
                return;
            }

            try
            {
                logger.Warn(message, args);
            }
            catch (Exception ex)
            {
                DebugOutputPanel.AddMessage(PluginManager.MessageType.Error, "[Ragnarok][RagnarokLogger.Warn] ERROR: " + ex.Message);
            }
        }
    }
}
