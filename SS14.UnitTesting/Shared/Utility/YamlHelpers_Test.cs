using NUnit.Framework;
using SS14.Shared.Utility;
using System;
using System.Globalization;
using YamlDotNet.RepresentationModel;

namespace SS14.UnitTesting.Shared.Utility
{
    [TestFixture]
    public class YamlHelpers_Test : SS14UnitTest
    {
        [Test]
        [SetCulture("fr-FR")]
        public void Test_CultureInvariance()
        {
            // Make sure that we're on a locale in which the decimals would be messed up.
            // French is one but I'd rather have false negatives than false positives.
            Assert.That(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, Is.EqualTo(","));
            Assert.That(float.Parse("10,5"), Is.EqualTo(10.5f));
            Assert.That(() => float.Parse("10.5"), Throws.InstanceOf<FormatException>());

            Assert.That(new YamlScalarNode("10.5").AsFloat(), Is.EqualTo(10.5f));
        }
    }
}
