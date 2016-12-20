namespace SexyFishHorse.CitiesSkylines.Ragnarok.UnitTest
{
    using System.Reflection;
    using FluentAssertions;
    using Xunit;

    public class RagnarokUserModClass
    {
        public class PrivateFieldsCheck
        {
            [Fact]
            public void WarningPhasePanelShouldHavePrivateFieldForIsEvacuating()
            {
                var evacuatingField = typeof(WarningPhasePanel).GetField("m_isEvacuating", BindingFlags.NonPublic | BindingFlags.Instance);

                evacuatingField.Should().NotBeNull();
            }

            [Fact]
            public void DisasterWrapperShouldHavePrivateFieldForDisasterTypeInfoConvertion()
            {
                var convertionField = typeof(DisasterWrapper).GetField(
                    "m_DisasterTypeToInfoConversion",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                convertionField.Should().NotBeNull();
            }
        }
    }
}
