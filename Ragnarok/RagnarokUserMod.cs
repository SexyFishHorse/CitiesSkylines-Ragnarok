namespace SexyFishHorse.CitiesSkylines.Ragnarok
{
    using System.Collections.Generic;
    using System.Reflection;
    using ICities;
    using Infrastructure;
    using Infrastructure.Configuration;
    using Infrastructure.UI;
    using JetBrains.Annotations;
    using Logger;

    [UsedImplicitly]
    public class RagnarokUserMod : UserModBase, IDisasterExtension
    {
        private const string ModName = "Ragnarok";

        private readonly ILogger logger;

        private DisasterWrapper disasterWrapper;

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

            disasterWrapper = (DisasterWrapper)disaster;

            DisasterManager.instance.m_disableAutomaticFollow = !ConfigurationManager.Instance.GetSetting<bool>(SettingKeys.DisableAutofocusDisaster);

            var field = disasterWrapper.GetType()
                                       .GetField("m_DisasterTypeToInfoConversion", BindingFlags.NonPublic | BindingFlags.Instance);

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

        public void OnDisasterActivated(ushort disasterId)
        {
        }

        public void OnDisasterCreated(ushort disasterId)
        {
            var disasterInfo = disasterWrapper.GetDisasterSettings(disasterId);
            logger.Info("Created disaster type {0}, name: {1}", disasterInfo.type, disasterInfo.name);

            var isCustom = disasterWrapper.IsCustomDisaster(disasterId);
            logger.Info("is custom? {0}", isCustom);

            var settingKey = GetDisabledSettingKeyForDisasterType(disasterInfo.type);

            if (settingKey == null)
            {
                logger.Info("No setting key for type");
                return;
            }

            if (ConfigurationManager.Instance.GetSetting<bool>(settingKey))
            {
                logger.Info("Deactivating disaster");
                disasterWrapper.DeactivateDisaster(disasterId);
            }
        }

        public void OnDisasterDeactivated(ushort disasterId)
        {
            // todo: stop evac logic
        }

        public void OnDisasterDetected(ushort disasterId)
        {
            // todo: start evac logic
        }

        public void OnDisasterFinished(ushort disasterId)
        {
        }

        public void OnDisasterStarted(ushort disasterId)
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

        private void OnDisableAutoFocusDisasterChanged(bool isChecked)
        {
            SaveSetting(SettingKeys.DisableAutofocusDisaster, isChecked);

            if (DisasterManager.exists)
            {
                DisasterManager.instance.m_disableAutomaticFollow = !isChecked;

                logger.Info("disableAutomaticFollow is " + DisasterManager.instance.m_disableAutomaticFollow);
            }
        }

        private void SaveSetting(string settingKey, object value)
        {
            logger.Info("Saving setting {0} with value {1}", settingKey, value);

            ConfigurationManager.Instance.SaveSetting(settingKey, value);
        }
    }
}
