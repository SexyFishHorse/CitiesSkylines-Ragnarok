namespace SexyFishHorse.CitiesSkylines.Ragnarok
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using ICities;
    using JetBrains.Annotations;
    using SexyFishHorse.CitiesSkylines.Infrastructure;
    using SexyFishHorse.CitiesSkylines.Infrastructure.Extensions;
    using SexyFishHorse.CitiesSkylines.Logger;
    using Object = UnityEngine.Object;

    [UsedImplicitly]
    public class RagnarokUserMod : UserModBase, IDisasterExtension, ILoadingExtension
    {
        public const string ModName = "Ragnarok";

        private readonly ILogger logger;

        private readonly HashSet<ushort> manualReleaseDisasters = new HashSet<ushort>();

        private FieldInfo convertionField;

        private DisasterWrapper disasterWrapper;

        private FieldInfo evacuatingField;

        private WarningPhasePanel phasePanel;

        public RagnarokUserMod()
        {
            try
            {
                logger = LogManager.Instance.GetOrCreateLogger(ModName);

                logger.Info("Ragnarok created");
                ModConfig.Instance.Logger = logger;
                ModConfig.Instance.Migrate<int>("AutoEvacuateEarthquake", SettingKeys.Earthquakes.AutoEvacuate);

                OptionsPanelManager = new OptionsPanelManager(logger);
            }
            catch (Exception ex)
            {
                logger.LogException(ex);
            }
        }

        public override string Description
        {
            get { return "More disaster controls"; }
        }

        public override string Name
        {
            get { return ModName; }
        }

        public static void UpdateAutoFollowDisaster(ILogger logger)
        {
            try
            {
                if (DisasterManager.exists)
                {
                    DisasterManager.instance.m_disableAutomaticFollow =
                        !ModConfig.Instance.GetSetting<bool>(SettingKeys.DisableAutofocusDisaster);

                    logger.Info("disableAutomaticFollow is " + DisasterManager.instance.m_disableAutomaticFollow);
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
                logger.Info("OnCreated " + disaster);

                disasterWrapper = (DisasterWrapper) disaster;

                convertionField = disasterWrapper
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
            var info = disasterWrapper.GetDisasterSettings(disasterId);

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
                                var pauseStart = DateTime.UtcNow + TimeSpan.FromSeconds(2);

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

            var info = disasterWrapper.GetDisasterSettings(disasterId);

            logger.Info(
                "OnDisasterCreated. Id: {0}, Name: {1}, Type: {2}, Intensity: {3}",
                disasterId,
                info.name,
                info.type,
                info.intensity);

            try
            {
                var disasterInfo = disasterWrapper.GetDisasterSettings(disasterId);
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
            var info = disasterWrapper.GetDisasterSettings(disasterId);

            logger.Info(
                "OnDisasterDeactivated. Id: {0}, Name: {1}, Type: {2}, Intensity: {3}",
                disasterId,
                info.name,
                info.type,
                info.intensity);

            try
            {
                var disasterInfo = disasterWrapper.GetDisasterSettings(disasterId);

                if (disasterInfo.type == DisasterType.Empty)
                {
                    return;
                }

                if (!IsEvacuating())
                {
                    logger.Info("Not evacuating. Clear list of active manual release disasters");
                    manualReleaseDisasters.Clear();
                    return;
                }

                if (ShouldAutoRelease(disasterInfo.type) && !manualReleaseDisasters.Any())
                {
                    logger.Info("Auto releasing citizens");
                    DisasterManager.instance.EvacuateAll(true);
                }
            }
            catch (Exception ex)
            {
                logger.LogException(ex);

                throw;
            }
        }

        public void OnDisasterDetected(ushort disasterId)
        {
            var info = disasterWrapper.GetDisasterSettings(disasterId);

            logger.Info(
                "OnDisasterDetected. Id: {0}, Name: {1}, Type: {2}, Intensity: {3}",
                disasterId,
                info.name,
                info.type,
                info.intensity);

            try
            {
                var disasterInfo = disasterWrapper.GetDisasterSettings(disasterId);

                if (disasterInfo.type == DisasterType.Empty)
                {
                    return;
                }

                if (ShouldAutoEvacuate(disasterInfo.type))
                {
                    logger.Info("Is auto-evacuate disaster");
                    if (!IsEvacuating())
                    {
                        logger.Info("Starting evacuation");
                        DisasterManager.instance.EvacuateAll(false);
                    }
                    else
                    {
                        logger.Info("Already evacuating");
                    }

                    if (ShouldManualRelease(disasterInfo.type))
                    {
                        logger.Info("Should be manually released");
                        manualReleaseDisasters.Add(disasterId);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogException(ex);

                throw;
            }
        }

        public void OnDisasterFinished(ushort disasterId)
        {
            var info = disasterWrapper.GetDisasterSettings(disasterId);

            logger.Info(
                "OnDisasterFinished. Id: {0}, Name: {1}, Type: {2}, Intensity: {3}",
                disasterId,
                info.name,
                info.type,
                info.intensity);
        }

        public void OnDisasterStarted(ushort disasterId)
        {
            SetConvertionTable();

            var info = disasterWrapper.GetDisasterSettings(disasterId);

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
                disasterWrapper.EndDisaster(disasterId);
            }

            if (ModConfig.Instance.GetSetting<bool>(settingKeys.ToggleMaxIntensity))
            {
                logger.Info("disable when over intensity {0}", ModConfig.Instance.GetSetting<byte>(settingKeys.MaxIntensity));
                if (info.intensity > ModConfig.Instance.GetSetting<byte>(settingKeys.MaxIntensity))
                {
                    logger.Info("Deactivating disaster");
                    disasterWrapper.EndDisaster(disasterId);
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

                FindPhasePanel();

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

        private void FindPhasePanel()
        {
            if (phasePanel == null)
            {
                phasePanel = Object.FindObjectOfType<WarningPhasePanel>();
                evacuatingField = phasePanel.GetType().GetField("m_isEvacuating", BindingFlags.NonPublic | BindingFlags.Instance);
            }
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

        private bool IsEvacuating()
        {
            FindPhasePanel();

            var isEvacuating = (bool) evacuatingField.GetValue(phasePanel);

            logger.Info("Is evacuating: " + isEvacuating);

            return isEvacuating;
        }

        void IDisasterExtension.OnReleased()
        {
        }

        private void SetConvertionTable()
        {
            var fieldValue = (Dictionary<DisasterType, DisasterInfo>) convertionField.GetValue(disasterWrapper);

            if (fieldValue == null || !fieldValue.Any() || fieldValue.Any(x => x.Value == null))
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

                convertionField.SetValue(disasterWrapper, convertionDictionary);
            }
        }

        private bool ShouldAutoEvacuate(DisasterType disasterType)
        {
            var settingKey = SettingKeys.AutoEvacuateSettingKeyMapping.Single(x => x.Key == disasterType).Value;

            return ModConfig.Instance.GetSetting<int>(settingKey) > 0;
        }

        private bool ShouldAutoRelease(DisasterType disasterType)
        {
            var settingKey = SettingKeys.AutoEvacuateSettingKeyMapping.Single(x => x.Key == disasterType).Value;

            return ModConfig.Instance.GetSetting<int>(settingKey) > 1;
        }

        private bool ShouldManualRelease(DisasterType disasterType)
        {
            var settingKey = SettingKeys.AutoEvacuateSettingKeyMapping.Single(x => x.Key == disasterType).Value;

            return ModConfig.Instance.GetSetting<int>(settingKey) == 1;
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
                disasterWrapper.EndDisaster(disasterId);
            }
            return false;
        }
    }
}
