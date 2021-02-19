using System.Globalization;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.TypeSerializers;

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers
{
    [TestFixture]
    [TestOf(typeof(AngleSerializer))]
    public class AngleSerializerTest : TypeSerializerTest
    {
        [Test]
        public void SerializationTest()
        {
            var degrees = 75d;
            var angle = Angle.FromDegrees(degrees);
            var node = (ValueDataNode) Serialization.WriteValue(angle);
            var serializedValue = $"{MathHelper.DegreesToRadians(degrees)} rad";

            Assert.That(node.Value, Is.EqualTo(serializedValue));
        }

        [Test]
        public void DeserializationTest()
        {
            var degrees = 75;
            var node = new ValueDataNode(degrees.ToString());
            var deserializedAngle = Serialization.ReadValue<Angle>(node);
            var angle = Angle.FromDegrees(degrees);

            Assert.That(deserializedAngle, Is.EqualTo(angle));
        }
    }
}
