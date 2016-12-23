namespace SexyFishHorse.CitiesSkylines.Ragnarok
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using Configuration;
    using ICities;
    using Infrastructure;
    using Infrastructure.Extensions;
    using JetBrains.Annotations;
    using Logger;
    using Logging;

    [UsedImplicitly]
    public class RagnarokUserMod : UserModBase, IDisasterExtension, ILoadingExtension
    {
        public const string ModName = "Ragnarok";

        private readonly ILogger logger;

        private FieldInfo convertionField;

        public RagnarokUserMod()
        {
            try
            {
                logger = RagnarokLogger.Instance;
                ModConfig.Instance.SaveSetting(SettingKeys.EnableLogging, true);
                logger.Info("Ragnarok created");

                ModConfig.Instance.Logger = logger;
                MigrateOldSettings();

                OptionsPanelManager = new OptionsPanelManager(logger);
            }
            catch (Exception ex)
            {
                logger.LogException(ex);

                throw;
            }
        }

        private static void MigrateOldSettings()
        {
            ModConfig.Instance.MigrateKey<int>("AutoEvacuateEarthquake", SettingKeys.Earthquakes.AutoEvacuate);
            ModConfig.Instance.MigrateType<int, byte>(SettingKeys.Earthquakes.MaxIntensity, Convert.ToByte);
            ModConfig.Instance.MigrateType<int, byte>(SettingKeys.ForestFires.MaxIntensity, Convert.ToByte);
            ModConfig.Instance.MigrateType<int, byte>(SettingKeys.Meteors.MaxIntensity, Convert.ToByte);
            ModConfig.Instance.MigrateType<int, byte>(SettingKeys.Sinkholes.MaxIntensity, Convert.ToByte);
            ModConfig.Instance.MigrateType<int, byte>(SettingKeys.StructureCollapses.MaxIntensity, Convert.ToByte);
            ModConfig.Instance.MigrateType<int, byte>(SettingKeys.StructureFires.MaxIntensity, Convert.ToByte);
            ModConfig.Instance.MigrateType<int, byte>(SettingKeys.Thunderstorms.MaxIntensity, Convert.ToByte);
            ModConfig.Instance.MigrateType<int, byte>(SettingKeys.Tornadoes.MaxIntensity, Convert.ToByte);
            ModConfig.Instance.MigrateType<int, byte>(SettingKeys.Tsunamis.MaxIntensity, Convert.ToByte);
        }

        public override string Description
        {
            get
            {
                return "More disaster controls";
            }
        }

        public override string Name
        {
            get
            {
                return ModName;
            }
        }

        public DisasterWrapper Wrapper { get; set; }

        public static void UpdateAutoFollowDisaster(ILogger logger)
        {
            try
            {
                if (DisasterManager.exists)
                {
                    DisasterManager.instance.m_disableAutomaticFollow =
                        !ModConfig.Instance.GetSetting<bool>(SettingKeys.DisableAutofocusDisaster);

                    logger.Info("disableAutomaticFollow is {0}", DisasterManager.instance.m_disableAutomaticFollow);
                }
            }
            catch (Exception ex)
            {
                logger.LogException(ex);

                throw;
            }
        }

        public void OnCreated(IDisaster disaster)
        {
            try
            {
                logger.Info("OnCreated {0}", disaster);

                Wrapper = (DisasterWrapper)disaster;

                convertionField = Wrapper
                    .GetType()
                    .GetField("m_DisasterTypeToInfoConversion", BindingFlags.NonPublic | BindingFlags.Instance);

                SetConvertionTable();
            }
            catch (Exception ex)
            {
                logger.LogException(ex);

                throw;
            }
        }

        public void OnCreated(ILoading loading)
        {
        }

        public void OnDisasterActivated(ushort disasterId)
        {
            var info = Wrapper.GetDisasterSettings(disasterId);

            logger.Info(
                "OnDisasterActivated. Id: {0}, Name: {1}, Type: {2}, Intensity: {3}",
                disasterId,
                info.name,
                info.type,
                info.intensity);

            if (TryDisableDisaster(disasterId, info))
            {
                return;
            }

            try
            {
                if (ModConfig.Instance.GetSetting<bool>(SettingKeys.PauseOnDisasterStart))
                {
                    new Thread(
                        () =>
                        {
                            try
                            {
                                var pauseStart = DateTime.UtcNow + TimeSpan.FromSeconds(5);

                                while (DateTime.UtcNow < pauseStart)
                                {
                                }

                                logger.Info("Pausing game");
                                SimulationManager.instance.SimulationPaused = true;
                            }
                            catch (Exception ex)
                            {
                                logger.LogException(ex);

                                throw;
                            }
                        }).Start();
                }
            }
            catch (Exception ex)
            {
                logger.LogException(ex);

                throw;
            }
        }

        public void OnDisasterCreated(ushort disasterId)
        {
            SetConvertionTable();

            var info = Wrapper.GetDisasterSettings(disasterId);

            logger.Info(
                "OnDisasterCreated. Id: {0}, Name: {1}, Type: {2}, Intensity: {3}",
                disasterId,
                info.name,
                info.type,
                info.intensity);

            try
            {
                var disasterInfo = Wrapper.GetDisasterSettings(disasterId);
                logger.Info(
                    "Created disaster type {0} with name {1} and intensity {2}",
                    disasterInfo.type,
                    disasterInfo.name,
                    disasterInfo.intensity);

                // TODO: Figure out why intensity is always 55 here.
                if (TryDisableDisaster(disasterId, disasterInfo))
                {
                }
            }
            catch (Exception ex)
            {
                logger.LogException(ex);

                throw;
            }
        }

        public void OnDisasterDeactivated(ushort disasterId)
        {
        }

        public void OnDisasterDetected(ushort disasterId)
        {
        }

        public void OnDisasterFinished(ushort disasterId)
        {
        }

        public void OnDisasterStarted(ushort disasterId)
        {
            SetConvertionTable();

            var info = Wrapper.GetDisasterSettings(disasterId);

            logger.Info(
                "OnDisasterStarted. Id: {0}, Name: {1}, Type: {2}, Intensity: {3}",
                disasterId,
                info.name,
                info.type,
                info.intensity);

            var settingKeys = GetSettingKeysForDisasterType(info.type);
            if (settingKeys == null)
            {
                logger.Info("No setting keys found");
                return;
            }

            if (ModConfig.Instance.GetSetting<bool>(settingKeys.Disable))
            {
                logger.Info("Deactivating disaster");
                Wrapper.EndDisaster(disasterId);
            }

            if (ModConfig.Instance.GetSetting<bool>(settingKeys.ToggleMaxIntensity))
            {
                logger.Info("disable when over intensity {0}", ModConfig.Instance.GetSetting<byte>(settingKeys.MaxIntensity));
                if (info.intensity > ModConfig.Instance.GetSetting<byte>(settingKeys.MaxIntensity))
                {
                    logger.Info("Deactivating disaster");
                    Wrapper.EndDisaster(disasterId);
                }
            }
        }

        public void OnLevelLoaded(LoadMode mode)
        {
            try
            {
                if (!mode.IsGameOrScenario())
                {
                    return;
                }

                if (mode.IsScenario())
                {
                    if (ModConfig.Instance.GetSetting<bool>(SettingKeys.DisableScenarioDisasters))
                    {
                        DisasterManager.instance.ClearAll();
                    }
                }

                BuildingManager.instance.m_firesDisabled = ModConfig.Instance.GetSetting<bool>(SettingKeys.DisableNonDisasterFires);

                UpdateAutoFollowDisaster(logger);
            }
            catch (Exception ex)
            {
                logger.LogException(ex);

                throw;
            }
        }

        public void OnLevelUnloading()
        {
        }

        public void OnReleased()
        {
        }

        void IDisasterExtension.OnReleased()
        {
        }

        private DisasterSettingKeys GetSettingKeysForDisasterType(DisasterType type)
        {
            switch (type)
            {
                case DisasterType.Earthquake:
                    return SettingKeys.Earthquakes;
                case DisasterType.ForestFire:
                    return SettingKeys.ForestFires;
                case DisasterType.MeteorStrike:
                    return SettingKeys.Meteors;
                case DisasterType.Sinkhole:
                    return SettingKeys.Sinkholes;
                case DisasterType.StructureCollapse:
                    return SettingKeys.StructureCollapses;
                case DisasterType.StructureFire:
                    return SettingKeys.StructureFires;
                case DisasterType.ThunderStorm:
                    return SettingKeys.Thunderstorms;
                case DisasterType.Tornado:
                    return SettingKeys.Tornadoes;
                case DisasterType.Tsunami:
                    return SettingKeys.Tsunamis;
                default:
                    return null;
            }
        }

        private void SetConvertionTable()
        {
            var fieldValue = (Dictionary<DisasterType, DisasterInfo>)convertionField.GetValue(Wrapper);

            if ((fieldValue == null) || !fieldValue.Any() || fieldValue.Any(x => x.Value == null))
            {
                logger.Info("rebuilding convertion table");
                var convertionDictionary = new Dictionary<DisasterType, DisasterInfo>();
                convertionDictionary[DisasterType.Earthquake] = DisasterManager.FindDisasterInfo<EarthquakeAI>();
                convertionDictionary[DisasterType.ForestFire] = DisasterManager.FindDisasterInfo<ForestFireAI>();
                convertionDictionary[DisasterType.MeteorStrike] = DisasterManager.FindDisasterInfo<MeteorStrikeAI>();
                convertionDictionary[DisasterType.ThunderStorm] = DisasterManager.FindDisasterInfo<ThunderStormAI>();
                convertionDictionary[DisasterType.Tornado] = DisasterManager.FindDisasterInfo<TornadoAI>();
                convertionDictionary[DisasterType.Tsunami] = DisasterManager.FindDisasterInfo<TsunamiAI>();
                convertionDictionary[DisasterType.StructureCollapse] = DisasterManager.FindDisasterInfo<StructureCollapseAI>();
                convertionDictionary[DisasterType.StructureFire] = DisasterManager.FindDisasterInfo<StructureFireAI>();
                convertionDictionary[DisasterType.Sinkhole] = DisasterManager.FindDisasterInfo<SinkholeAI>();

                if (convertionDictionary.Any(x => x.Value == null))
                {
                    logger.Info("Contains null values");
                }

                convertionField.SetValue(Wrapper, convertionDictionary);
            }
        }

        private bool TryDisableDisaster(ushort disasterId, DisasterSettings disasterInfo)
        {
            var settingKeys = GetSettingKeysForDisasterType(disasterInfo.type);

            if (settingKeys == null)
            {
                logger.Info("No setting keys found");
                return true;
            }

            if (ModConfig.Instance.GetSetting<bool>(settingKeys.Disable))
            {
                logger.Info("Deactivating disaster");
                Wrapper.EndDisaster(disasterId);
            }

            return false;
        }
    }
}
