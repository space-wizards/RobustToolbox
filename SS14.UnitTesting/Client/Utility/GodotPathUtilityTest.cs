using NUnit.Framework;
using SS14.Client.Utility;
using SS14.Shared.Utility;

namespace SS14.UnitTesting.Client.Utility
{
    [Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
    [TestFixture]
    [TestOf(typeof(GodotPathUtility))]
    public class GodotPathUtilityTest
    {
        [Test]
        public void Test()
        {
            Assert.That(GodotPathUtility.GodotPathToResourcePath("res://Engine/Scenes"),
                Is.EqualTo(new ResourcePath("/Scenes")));

            Assert.That(GodotPathUtility.GodotPathToResourcePath("res://Content/Scenes"),
                Is.EqualTo(new ResourcePath("/Scenes")));

            Assert.That(() => GodotPathUtility.GodotPathToResourcePath("res://Scenes"),
                Throws.ArgumentException);
        }
    }
}
