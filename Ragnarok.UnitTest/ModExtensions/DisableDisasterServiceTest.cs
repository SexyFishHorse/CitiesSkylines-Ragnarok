namespace SexyFishHorse.CitiesSkylines.Ragnarok.UnitTest.ModExtensions
{
    using System.Reflection;
    using FluentAssertions;
    using JetBrains.Annotations;
    using Xunit;

    [UsedImplicitly]
    public class DisableDisasterServiceTest
    {
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
