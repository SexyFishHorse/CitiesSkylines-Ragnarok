namespace SexyFishHorse.CitiesSkylines.Ragnarok
{
    using Infrastructure.Configuration;

    public static class ModConfig
    {
        private static ConfigurationManager instance;

        public static ConfigurationManager Instance
        {
            get
            {
                return instance ?? (instance = new ConfigurationManager(RagnarokUserMod.ModName));
            }
        }
    }
}
