namespace SexyFishHorse.CitiesSkylines.Ragnarok.ModExtensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Configuration;
    using ICities;
    using Infrastructure.Extensions;
    using JetBrains.Annotations;
    using Logger;
    using Logging;

    [UsedImplicitly]
    public class DisableDisasterService : IDisasterBase, ILoadingExtension
    {
        private readonly ILogger logger;

        private FieldInfo convertionField;

        private DisasterWrapper disasterWrapper;

        public DisableDisasterService()
        {
            logger = RagnarokLogger.Instance;
            logger.Info("DisableDisasterService created");
        }

        public override void OnCreated(IDisaster disaster)
        {
            try
            {
                logger.Info("DDS: OnCreated {0}", disaster);

                disasterWrapper = (DisasterWrapper)disaster;

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

        public override void OnDisasterActivated(ushort disasterId)
        {
            try
            {
                var info = disasterWrapper.GetDisasterSettings(disasterId);

                logger.Info(
                    "DDS: OnDisasterActivated. Id: {0}, Name: {1}, Type: {2}, Intensity: {3}",
                    disasterId,
                    info.name,
                    info.type,
                    info.intensity);

                if (TryDisableDisaster(disasterId, info))
                {
                    return;
                }

                GenericDisasterServices.HandlePauseOnDisaster(logger);

            }
            catch (Exception ex)
            {
                logger.LogException(ex);

                throw;
            }
        }

        public override void OnDisasterCreated(ushort disasterId)
        {
            SetConvertionTable();

            var info = disasterWrapper.GetDisasterSettings(disasterId);

            logger.Info(
                "DDS: OnDisasterCreated. Id: {0}, Name: {1}, Type: {2}, Intensity: {3}",
                disasterId,
                info.name,
                info.type,
                info.intensity);

            try
            {
                var disasterInfo = disasterWrapper.GetDisasterSettings(disasterId);
                logger.Info(
                    "DDS: Created disaster type {0} with name {1} and intensity {2}",
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

        public override void OnDisasterStarted(ushort disasterId)
        {
            SetConvertionTable();

            var info = disasterWrapper.GetDisasterSettings(disasterId);

            logger.Info(
                "DDS: OnDisasterStarted. Id: {0}, Name: {1}, Type: {2}, Intensity: {3}",
                disasterId,
                info.name,
                info.type,
                info.intensity);

            var settingKeys = GetSettingKeysForDisasterType(info.type);
            if (settingKeys == null)
            {
                logger.Info("DDS: No setting keys found");
                return;
            }

            if (ModConfig.Instance.GetSetting<bool>(settingKeys.Disable))
            {
                logger.Info("DDS: Deactivating disaster");
                disasterWrapper.EndDisaster(disasterId);
            }

            var maxIntensity = ModConfig.Instance.GetSetting<byte>(settingKeys.MaxIntensity);
            if (maxIntensity > 0)
            {
                logger.Info("DDS: Disable when over intensity {0}", maxIntensity);
                if (info.intensity > maxIntensity)
                {
                    logger.Info("DDS: Deactivating disaster");
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

                BuildingManager.instance.m_firesDisabled = ModConfig.Instance.GetSetting<bool>(SettingKeys.DisableNonDisasterFires);

                GenericDisasterServices.UpdateAutoFollowDisaster(logger);
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
            var fieldValue = (Dictionary<DisasterType, DisasterInfo>)convertionField.GetValue(disasterWrapper);

            if ((fieldValue == null) || !fieldValue.Any() || fieldValue.Any(x => x.Value == null))
            {
                logger.Info("DDS: Rebuilding convertion table");
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
                    logger.Info("DDS: Contains null values");
                }

                convertionField.SetValue(disasterWrapper, convertionDictionary);
            }
        }

        private bool TryDisableDisaster(ushort disasterId, DisasterSettings disasterInfo)
        {
            var settingKeys = GetSettingKeysForDisasterType(disasterInfo.type);

            if (settingKeys == null)
            {
                logger.Info("DDS: No setting keys found");
                return true;
            }

            if (ModConfig.Instance.GetSetting<bool>(settingKeys.Disable))
            {
                logger.Info("DDS: Deactivating disaster");
                disasterWrapper.EndDisaster(disasterId);
            }

            return false;
        }
    }
}
