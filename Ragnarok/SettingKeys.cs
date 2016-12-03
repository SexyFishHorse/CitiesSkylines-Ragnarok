namespace SexyFishHorse.CitiesSkylines.Ragnarok
{
    using System.Collections.Generic;
    using ICities;

    public static class SettingKeys
    {
        public const string AutoEvacuateEarthquake = "AutoEvacuateEarthquake";

        public const string AutoEvacuateForestFires = "AutoEvacuateForestFires";

        public const string AutoEvacuateMeteors = "AutoEvacuateMeteors";

        public const string AutoEvacuateSinkholes = "AutoEvacuateSinkholes";

        public const string AutoEvacuateStructureCollapses = "AutoEvacuateStructureCollapses";

        public const string AutoEvacuateStructureFires = "AutoEvacuateStructureFires";

        public const string AutoEvacuateThunderstorms = "AutoEvacuateThunderstorms";

        public const string AutoEvacuateTornadoes = "AutoEvacuateTornadoes";

        public const string AutoEvacuateTsunamis = "AutoEvacuateTsunamis";

        public const string DisableAutofocusDisaster = "DisableAutofocusDisaster";

        public const string DisableEarthquakes = "DisableEarthquakes";

        public const string DisableForestFires = "DisableForestFires";

        public const string DisableMeteors = "DisableMeteors";

        public const string DisableNonDisasterFires = "DisableNonDisasterFires";

        public const string DisableScenarioDisasters = "DisableScenarioDisasters";

        public const string DisableSinkholes = "DisableSinkholes";

        public const string DisableStructureCollapses = "DisableStructureCollapses";

        public const string DisableStructureFires = "DisableStructureFires";

        public const string DisableThunderstorms = "DisableThunderstorms";

        public const string DisableTornadoes = "DisableTornadoes";

        public const string DisableTsunamis = "DisableTsunamis";

        public static readonly IDictionary<DisasterType, string> AutoEvacuateSettingKeyMapping = new Dictionary
            <DisasterType, string>
            {
                {DisasterType.Earthquake, AutoEvacuateEarthquake},
                {DisasterType.ForestFire, AutoEvacuateForestFires},
                {DisasterType.MeteorStrike, AutoEvacuateMeteors},
                {DisasterType.Sinkhole, AutoEvacuateSinkholes},
                {DisasterType.StructureCollapse, AutoEvacuateStructureCollapses},
                {DisasterType.StructureFire, AutoEvacuateStructureFires},
                {DisasterType.ThunderStorm, AutoEvacuateThunderstorms},
                {DisasterType.Tornado, AutoEvacuateTornadoes},
                {DisasterType.Tsunami, AutoEvacuateTsunamis}
            };

        public static readonly IDictionary<DisasterType, string> DisableDisasterSettingKeyMapping = new Dictionary
            <DisasterType, string>
            {
                {DisasterType.Earthquake, DisableEarthquakes},
                {DisasterType.ForestFire, DisableForestFires},
                {DisasterType.MeteorStrike, DisableMeteors},
                {DisasterType.Sinkhole, DisableSinkholes},
                {DisasterType.StructureCollapse, DisableStructureCollapses},
                {DisasterType.StructureFire, DisableStructureFires},
                {DisasterType.ThunderStorm, DisableThunderstorms},
                {DisasterType.Tornado, DisableTornadoes},
                {DisasterType.Tsunami, DisableTsunamis}
            };
    }
}
