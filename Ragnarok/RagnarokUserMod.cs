namespace SexyFishHorse.CitiesSkylines.Ragnarok
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using ICities;
    using Infrastructure;
    using Infrastructure.Configuration;
    using Infrastructure.UI;
    using JetBrains.Annotations;
    using Logger;
    using UnityEngine;
    using ILogger = Logger.ILogger;

    [UsedImplicitly]
    public class RagnarokUserMod : UserModBase, IDisasterExtension, ILoadingExtension
    {
        private const string ModName = "Ragnarok";

        private static readonly string[] AutoEvacuateValues =
        {
            "Disabled",
            "Enabled - manual release",
            "Enabled - auto release"
        };

        private readonly ILogger logger;

        private readonly HashSet<ushort> manualReleaseDisasters = new HashSet<ushort>();

        private DisasterWrapper disasterWrapper;

        private WarningPhasePanel phasePanel;

        static RagnarokUserMod()
        {
            ConfigurationManager.Instance.Init(ModName);
        }

        public RagnarokUserMod()
        {
            logger = LogManager.Instance.GetOrCreateLogger(ModName);

            logger.Info("Ragnarok created");
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

        public void OnCreated(IDisaster disaster)
        {
            logger.Info("OnCreated " + disaster);

            disasterWrapper = (DisasterWrapper) disaster;

            DisasterManager.instance.m_disableAutomaticFollow =
                !ConfigurationManager.Instance.GetSetting<bool>(SettingKeys.DisableAutofocusDisaster);

            logger.Info("disableautomaticfollow is " + DisasterManager.instance.m_disableAutomaticFollow);

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

            if (settingKey == null)
            {
                logger.Info("No setting key for type");
                return;
            }

            if (ConfigurationManager.Instance.GetSetting<bool>(settingKey))
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
            if ((mode != LoadMode.NewGame) || (mode != LoadMode.LoadGame))
            {
                return;
            }

            phasePanel = Object.FindObjectOfType<WarningPhasePanel>();

            BuildingManager.instance.m_firesDisabled =
                ConfigurationManager.Instance.GetSetting<bool>(SettingKeys.DisableNonDisasterFires);
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

        protected override void ConfigureOptionsUi(IStronglyTypedUiHelper uiHelper)
        {
            var generalGroup = uiHelper.AddGroup("General");

            generalGroup.AddCheckBox("Auto focus on disaster",
                                     !ConfigurationManager.Instance.GetSetting<bool>(SettingKeys.DisableAutofocusDisaster),
                                     OnDisableAutoFocusDisasterChanged);

            var enabledDisastersGroup = uiHelper.AddGroup("Disable disasters");
            AddEnabledDisasterCheckbox(enabledDisastersGroup, "Forest fires", SettingKeys.DisableForestFires);
            AddEnabledDisasterCheckbox(enabledDisastersGroup, "Earthquakes", SettingKeys.DisableEarthquakes);
            AddEnabledDisasterCheckbox(enabledDisastersGroup, "Meteors", SettingKeys.DisableMeteors);
            AddEnabledDisasterCheckbox(enabledDisastersGroup, "Sinkholes", SettingKeys.DisableSinkholes);
            AddEnabledDisasterCheckbox(enabledDisastersGroup, "Structure collapses", SettingKeys.DisableStructureCollapses);
            AddEnabledDisasterCheckbox(enabledDisastersGroup, "Structure fires", SettingKeys.DisableStructureFires);
            AddEnabledDisasterCheckbox(enabledDisastersGroup, "Thunderstorms", SettingKeys.DisableThunderstorms);
            AddEnabledDisasterCheckbox(enabledDisastersGroup, "Tornadoes", SettingKeys.DisableTornadoes);
            AddEnabledDisasterCheckbox(enabledDisastersGroup, "Tsunamis", SettingKeys.DisableTsunamis);

            enabledDisastersGroup.AddCheckBox("Non-disaster related fires",
                                              ConfigurationManager.Instance.GetSetting<bool>(SettingKeys.DisableNonDisasterFires),
                                              OnDisableNonDisasterFiresChanged);

            var autoEvacuateGroup = uiHelper.AddGroup("Auto-evacuation behaviour");

            AddAutoEvacuateBehaviourDropDown(autoEvacuateGroup, "Forest fires", SettingKeys.AutoEvacuateForestFires);
            AddAutoEvacuateBehaviourDropDown(autoEvacuateGroup, "Earthquakes", SettingKeys.AutoEvacuateEarthquake);
            AddAutoEvacuateBehaviourDropDown(autoEvacuateGroup, "Meteors", SettingKeys.AutoEvacuateMeteors);
            AddAutoEvacuateBehaviourDropDown(autoEvacuateGroup, "Sinkholes", SettingKeys.AutoEvacuateSinkholes);
            AddAutoEvacuateBehaviourDropDown(autoEvacuateGroup, "Structure collapses", SettingKeys.AutoEvacuateStructureCollapses);
            AddAutoEvacuateBehaviourDropDown(autoEvacuateGroup, "Structure fires", SettingKeys.AutoEvacuateStructureFires);
            AddAutoEvacuateBehaviourDropDown(autoEvacuateGroup, "Thunderstorms", SettingKeys.AutoEvacuateThunderstorms);
            AddAutoEvacuateBehaviourDropDown(autoEvacuateGroup, "Tornadoes", SettingKeys.AutoEvacuateTornadoes);
            AddAutoEvacuateBehaviourDropDown(autoEvacuateGroup, "Tsunamis", SettingKeys.AutoEvacuateTsunamis);
        }

        private void AddAutoEvacuateBehaviourDropDown(StronglyTypedUiHelper autoEvacuateGroup, string label,
                                                      string settingKey)
        {
            autoEvacuateGroup.AddDropDown(label, AutoEvacuateValues,
                                          ConfigurationManager.Instance.GetSetting<int>(settingKey),
                                          sel => SaveSetting(settingKey, sel));
        }

        private void AddEnabledDisasterCheckbox(StronglyTypedUiHelper uiGroup, string label, string settingKey)
        {
            uiGroup.AddCheckBox(label, ConfigurationManager.Instance.GetSetting<bool>(settingKey),
                                isChecked => SaveSetting(settingKey, isChecked));
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
            var field = phasePanel.GetType().GetField("m_isEvacuating", BindingFlags.NonPublic | BindingFlags.Instance);

            var isEvacuating = (bool) field.GetValue(phasePanel);

            logger.Info("Is evacuating: " + isEvacuating);

            return isEvacuating;
        }

        private void OnDisableAutoFocusDisasterChanged(bool isChecked)
        {
            SaveSetting(SettingKeys.DisableAutofocusDisaster, isChecked);

            if (DisasterManager.exists)
            {
                DisasterManager.instance.m_disableAutomaticFollow = !isChecked;

                logger.Info("disableAutomaticFollow is " + DisasterManager.instance.m_disableAutomaticFollow);
            }
        }

        private void OnDisableNonDisasterFiresChanged(bool isChecked)
        {
            SaveSetting(SettingKeys.DisableNonDisasterFires, isChecked);

            if (BuildingManager.exists)
            {
                BuildingManager.instance.m_firesDisabled = isChecked;
            }
        }

        private void SaveSetting(string settingKey, object value)
        {
            logger.Info("Saving setting {0} with value {1}", settingKey, value);

            ConfigurationManager.Instance.SaveSetting(settingKey, value);
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

            return ConfigurationManager.Instance.GetSetting<int>(settingKey) > 0;
        }

        private bool ShouldAutoRelease(DisasterType disasterType)
        {
            var settingKey = SettingKeys.AutoEvacuateSettingKeyMapping.Single(x => x.Key == disasterType).Value;

            return ConfigurationManager.Instance.GetSetting<int>(settingKey) > 1;
        }

        private bool ShouldManualRelease(DisasterType disasterType)
        {
            var settingKey = SettingKeys.AutoEvacuateSettingKeyMapping.Single(x => x.Key == disasterType).Value;

            return ConfigurationManager.Instance.GetSetting<int>(settingKey) == 1;
        }
    }
}
