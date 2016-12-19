namespace SexyFishHorse.CitiesSkylines.Ragnarok
{
    using System.Collections.Generic;
    using System.Globalization;
    using ColossalFramework.UI;
    using Infrastructure.Common;
    using Infrastructure.UI;
    using Infrastructure.UI.Configuration;
    using Infrastructure.UI.Extensions;
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

        private readonly IDictionary<string, UISlider> maxIntensitySliders = new Dictionary<string, UISlider>();

        public OptionsPanelManager(ILogger logger)
        {
            this.logger = logger;

            logger.Info("OptionsPanelManager created");
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

            var disasters = new List<Tuple<string, DisasterSettingKeys>>
            {
                Tuple.Create("Earthquakes", SettingKeys.Earthquakes),
                Tuple.Create("Forest fires", SettingKeys.ForestFires),
                Tuple.Create("Meteors", SettingKeys.Meteors),
                Tuple.Create("Sinkholes", SettingKeys.Sinkholes),
                Tuple.Create("Building collapses", SettingKeys.StructureCollapses),
                Tuple.Create("Disaster fires", SettingKeys.StructureFires),
                Tuple.Create("Thunderstorms", SettingKeys.Thunderstorms),
                Tuple.Create("Tornadoes", SettingKeys.Tornadoes),
                Tuple.Create("Tsunamis", SettingKeys.Tsunamis)
            };

            foreach (var tuple in disasters)
            {
                var disasterGroup = uiHelper.AddGroup(tuple.Item1);
                AddEnabledDisasterCheckbox(disasterGroup, "Disable disaster", tuple.Item2.Disable);
                AddMaxIntensitySlider(disasterGroup, tuple.Item2.MaxIntensity, tuple.Item2.ToggleMaxIntensity);
                AddAutoEvacuateBehaviourDropDown(disasterGroup, tuple.Item2.AutoEvacuate);
            }

            var scenarioGroup = uiHelper.AddGroup("Scenarios");
            AddEnabledDisasterCheckbox(scenarioGroup, "Disable all disasters (really?)", SettingKeys.DisableScenarioDisasters);
        }

        private void AddAutoEvacuateBehaviourDropDown(
            StronglyTypedUiHelper autoEvacuateGroup,
            string settingKey)
        {
            autoEvacuateGroup.AddDropDown(
                "Auto evacuate behaviour",
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

        private void AddMaxIntensitySlider(StronglyTypedUiHelper group, string impactSettingKey, string toggleImpactSettingKey)
        {
            group.AddCheckBox(
                "Disable disasters over a certain intensity (use slider below)",
                ModConfig.Instance.GetSetting<bool>(toggleImpactSettingKey),
                isChecked =>
                {
                    ModConfig.Instance.SaveSetting(toggleImpactSettingKey, isChecked);
                    if (ModConfig.Instance.GetSetting<byte>(impactSettingKey) < 10)
                    {
                        ModConfig.Instance.SaveSetting(impactSettingKey, (byte)55);
                    }
                });

            var setting = ModConfig.Instance.GetSetting<byte>(impactSettingKey);

            if (setting < 10)
            {
                setting = 55;
            }

            var slider =
                group.AddSlider(
                    string.Format("Max Intensity ({0})", (setting / 10.0f).ToString("F1", CultureInfo.CurrentUICulture)),
                    10,
                    100,
                    1,
                    setting,
                    val =>
                    {
                        ModConfig.Instance.SaveSetting(impactSettingKey, (byte)val);
                        UpdateMaxIntensityLabel(impactSettingKey, val);
                    });

            if (maxIntensitySliders.ContainsKey(impactSettingKey))
            {
                maxIntensitySliders[impactSettingKey] = slider;
            }
            else
            {
                maxIntensitySliders.Add(impactSettingKey, slider);
            }
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

        private void UpdateMaxIntensityLabel(string impactSettingKey, float value)
        {
            maxIntensitySliders[impactSettingKey].SetLabelText(string.Format("Max intensity ({0})", (value / 10.0f).ToString("f1", CultureInfo.CurrentUICulture)));
        }
    }
}
