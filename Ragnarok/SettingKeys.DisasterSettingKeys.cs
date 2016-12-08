namespace SexyFishHorse.CitiesSkylines.Ragnarok
{
    public class DisasterSettingKeys
    {
        public DisasterSettingKeys(string disasterName)
        {
            AutoEvacuate = string.Format("AutoEvacuate{0}", disasterName);
            Disable = string.Format("Disable{0}", disasterName);
            MaxIntensity = string.Format("MaxIntensity{0}", disasterName);
            ToggleMaxIntensity = string.Format("ToggleMaxIntensity{0}", disasterName);
        }

        public string AutoEvacuate { get; private set; }

        public string Disable { get; private set; }

        public string MaxIntensity { get; private set; }

        public string ToggleMaxIntensity { get; private set; }
    }
}
