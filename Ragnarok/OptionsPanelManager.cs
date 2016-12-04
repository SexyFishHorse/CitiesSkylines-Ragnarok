namespace SexyFishHorse.CitiesSkylines.Ragnarok
{
    using System.Collections.Generic;
    using Infrastructure.Common;
    using Infrastructure.Configuration;
    using Infrastructure.UI;
    using Logger;

    public class OptionsPanelManager : IOptionsPanelManager
    {
        private static readonly string[] AutoEvacuateValues =
        {
            "Disabled",
            "Enabled - manual release",
            "Enabled - auto release"
        };

        private readonly ILogger logger;

        public OptionsPanelManager(ILogger logger)
        {
            this.logger = logger;
        }

        public void ConfigureOptionsPanel(IStronglyTypedUiHelper uiHelper)
        {
            var generalGroup = uiHelper.AddGroup("General");

            generalGroup.AddCheckBox(
                "Auto focus on disaster",
                ModConfig.Instance.GetSetting<bool>(SettingKeys.DisableAutofocusDisaster),
                OnAutoFocusDisasterChanged);

            generalGroup.AddCheckBox(
                "Pause on disaster start",
                ModConfig.Instance.GetSetting<bool>(SettingKeys.PauseOnDisasterStart),
                isChecked => SaveSetting(SettingKeys.PauseOnDisasterStart, isChecked));

            var nonDisasterFiresGroup = uiHelper.AddGroup("Non-disaster related fires");
            nonDisasterFiresGroup.AddCheckBox(
                "Disable",
                ModConfig.Instance.GetSetting<bool>(SettingKeys.DisableNonDisasterFires),
                OnDisableNonDisasterFiresChanged);

            var disasters = new List<Tuple<string, string, string>>
            {
                Tuple.Create("Earthquakes", SettingKeys.DisableEarthquakes, SettingKeys.AutoEvacuateEarthquake),
                Tuple.Create("Forest fires", SettingKeys.DisableForestFires, SettingKeys.AutoEvacuateForestFires),
                Tuple.Create("Meteors", SettingKeys.DisableMeteors, SettingKeys.AutoEvacuateMeteors),
                Tuple.Create("Sinkholes", SettingKeys.DisableSinkholes, SettingKeys.AutoEvacuateSinkholes),
                Tuple.Create("Building collapses", SettingKeys.DisableStructureCollapses, SettingKeys.AutoEvacuateStructureCollapses),
                Tuple.Create("Disaster fires", SettingKeys.DisableStructureFires, SettingKeys.AutoEvacuateStructureFires),
                Tuple.Create("Thunderstorms", SettingKeys.DisableThunderstorms, SettingKeys.AutoEvacuateThunderstorms),
                Tuple.Create("Tornadoes", SettingKeys.DisableTornadoes, SettingKeys.AutoEvacuateTornadoes),
                Tuple.Create("Tsunamis", SettingKeys.DisableTsunamis, SettingKeys.AutoEvacuateTsunamis)
            };

            foreach (var tuple in disasters)
            {
                var disasterGroup = uiHelper.AddGroup(tuple.Item1);
                AddEnabledDisasterCheckbox(disasterGroup, "Disable disaster", tuple.Item2);
                AddAutoEvacuateBehaviourDropDown(disasterGroup, "Auto evacuate behaviour", tuple.Item3);
            }

            var scenarioGroup = uiHelper.AddGroup("Scenarios");
            AddEnabledDisasterCheckbox(scenarioGroup, "Disable all disasters (really?)", SettingKeys.DisableScenarioDisasters);
        }

        private void AddAutoEvacuateBehaviourDropDown(StronglyTypedUiHelper autoEvacuateGroup, string label,
                                                      string settingKey)
        {
            autoEvacuateGroup.AddDropDown(
                label,
                AutoEvacuateValues,
                ModConfig.Instance.GetSetting<int>(settingKey),
                sel => SaveSetting(settingKey, sel));
        }

        private void AddEnabledDisasterCheckbox(StronglyTypedUiHelper uiGroup, string label, string settingKey)
        {
            uiGroup.AddCheckBox(
                label,
                ModConfig.Instance.GetSetting<bool>(settingKey),
                isChecked => SaveSetting(settingKey, isChecked));
        }

        private void OnAutoFocusDisasterChanged(bool isChecked)
        {
            SaveSetting(SettingKeys.DisableAutofocusDisaster, isChecked);

            RagnarokUserMod.UpdateAutoFollowDisaster(logger);
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

            ModConfig.Instance.SaveSetting(settingKey, value);
        }
    }
}
