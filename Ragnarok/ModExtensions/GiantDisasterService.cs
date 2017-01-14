namespace SexyFishHorse.CitiesSkylines.Ragnarok.ModExtensions
{
    using ColossalFramework.UI;
    using ICities;
    using SexyFishHorse.CitiesSkylines.Infrastructure.Extensions;
    using SexyFishHorse.CitiesSkylines.Ragnarok.Logging;
    using UnityEngine;
    using ILogger = SexyFishHorse.CitiesSkylines.Logger.ILogger;

    public class GiantDisasterService : ILoadingExtension
    {
        private readonly ILogger logger;

        public GiantDisasterService()
        {
            logger = RagnarokLogger.Instance;
        }

        public void OnCreated(ILoading loading)
        {
        }

        public void OnLevelLoaded(LoadMode mode)
        {
            if (!mode.IsGameOrScenario())
            {
                return;
            }

            logger.Info("Changing max disaster spawn intensity");

            var optionPanel = Object.FindObjectOfType<DisastersOptionPanel>();
            var slider = optionPanel.GetComponentInChildren<UISlider>();
            slider.maxValue = byte.MaxValue;

            logger.Info("Max disaster spawn intensity changedto 25");
        }

        public void OnLevelUnloading()
        {
        }

        public void OnReleased()
        {
        }
    }
}
