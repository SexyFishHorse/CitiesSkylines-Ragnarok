namespace SexyFishHorse.CitiesSkylines.Ragnarok
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using ICities;
    using Infrastructure;
    using JetBrains.Annotations;
    using Logger;
    using UnityEngine;
    using ILogger = Logger.ILogger;

    [UsedImplicitly]
    public class RagnarokUserMod : UserModBase, IDisasterExtension, ILoadingExtension
    {
        public const string ModName = "Ragnarok";

        private readonly ILogger logger;

        private readonly HashSet<ushort> manualReleaseDisasters = new HashSet<ushort>();

        private DisasterWrapper disasterWrapper;

        private WarningPhasePanel phasePanel;

        public RagnarokUserMod()
        {
            logger = LogManager.Instance.GetOrCreateLogger(ModName);

            logger.Info("Ragnarok created");

            OptionsPanelManager = new OptionsPanelManager(logger);
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

        public static void UpdateAutoFollowDisaster(ILogger logger)
        {
            if (DisasterManager.exists)
            {
                DisasterManager.instance.m_disableAutomaticFollow =
                    !ModConfig.Instance.GetSetting<bool>(SettingKeys.DisableAutofocusDisaster);

                logger.Info("disableAutomaticFollow is " + DisasterManager.instance.m_disableAutomaticFollow);
            }
        }

        public void OnCreated(IDisaster disaster)
        {
            logger.Info("OnCreated " + disaster);

            disasterWrapper = (DisasterWrapper)disaster;

            SetConvertionTable();
        }

        public void OnCreated(ILoading loading)
        {
        }

        public void OnDisasterActivated(ushort disasterId)
        {
        }

        public void OnDisasterCreated(ushort disasterId)
        {
            var disasterInfo = disasterWrapper.GetDisasterSettings(disasterId);
            logger.Info("Created disaster type {0} with name {1}", disasterInfo.type, disasterInfo.name);

            var settingKey = GetDisabledSettingKeyForDisasterType(disasterInfo.type);

            if (string.IsNullOrEmpty(settingKey))
            {
                logger.Info("No setting key found");
            }

            if (ModConfig.Instance.GetSetting<bool>(settingKey))
            {
                logger.Info("Deactivating disaster");
                disasterWrapper.EndDisaster(disasterId);
            }
        }

        public void OnDisasterDeactivated(ushort disasterId)
        {
            var disasterInfo = disasterWrapper.GetDisasterSettings(disasterId);

            logger.Info("Disaster {0} with name {1} over", disasterInfo.type, disasterInfo.name);

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

        public void OnDisasterDetected(ushort disasterId)
        {
            var disasterInfo = disasterWrapper.GetDisasterSettings(disasterId);

            logger.Info("Detected {0} with name {1}", disasterInfo.type, disasterInfo.name);

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

        public void OnDisasterFinished(ushort disasterId)
        {
        }

        public void OnDisasterStarted(ushort disasterId)
        {
        }

        public void OnLevelLoaded(LoadMode mode)
        {
            var updateMode = (SimulationManager.UpdateMode)mode;

            logger.Info("Level loaded: " + updateMode);

            if ((updateMode != SimulationManager.UpdateMode.NewGameFromMap) &&
                (updateMode != SimulationManager.UpdateMode.NewGameFromScenario) &&
                (updateMode != SimulationManager.UpdateMode.LoadGame) && (updateMode != SimulationManager.UpdateMode.LoadScenario))
            {
                return;
            }

            if ((updateMode == SimulationManager.UpdateMode.NewGameFromScenario) ||
                (updateMode == SimulationManager.UpdateMode.LoadScenario))
            {
                if (ModConfig.Instance.GetSetting<bool>(SettingKeys.DisableScenarioDisasters))
                {
                    DisasterManager.instance.ClearAll();
                }
            }

            phasePanel = Object.FindObjectOfType<WarningPhasePanel>();

            BuildingManager.instance.m_firesDisabled = ModConfig.Instance.GetSetting<bool>(SettingKeys.DisableNonDisasterFires);

            UpdateAutoFollowDisaster(logger);
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

        private string GetDisabledSettingKeyForDisasterType(DisasterType type)
        {
            switch (type)
            {
                case DisasterType.Earthquake:
                    return SettingKeys.DisableEarthquakes;
                case DisasterType.ForestFire:
                    return SettingKeys.DisableForestFires;
                case DisasterType.MeteorStrike:
                    return SettingKeys.DisableMeteors;
                case DisasterType.Sinkhole:
                    return SettingKeys.DisableSinkholes;
                case DisasterType.StructureCollapse:
                    return SettingKeys.DisableStructureCollapses;
                case DisasterType.StructureFire:
                    return SettingKeys.DisableStructureFires;
                case DisasterType.ThunderStorm:
                    return SettingKeys.DisableThunderstorms;
                case DisasterType.Tornado:
                    return SettingKeys.DisableTornadoes;
                case DisasterType.Tsunami:
                    return SettingKeys.DisableTsunamis;
                default:
                    return null;
            }
        }

        private bool IsEvacuating()
        {
            if (phasePanel == null)
            {
                phasePanel = Object.FindObjectOfType<WarningPhasePanel>();
            }

            var field = phasePanel.GetType().GetField("m_isEvacuating", BindingFlags.NonPublic | BindingFlags.Instance);

            var isEvacuating = (bool)field.GetValue(phasePanel);

            logger.Info("Is evacuating: " + isEvacuating);

            return isEvacuating;
        }

        private void SetConvertionTable()
        {
            var field = disasterWrapper.GetType()
                                       .GetField("m_DisasterTypeToInfoConversion",
                                                 BindingFlags.NonPublic | BindingFlags.Instance);

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
            field.SetValue(disasterWrapper, convertionDictionary);
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
    }
}
