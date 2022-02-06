using System.Globalization;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers
{
    [TestFixture]
    [TestOf(typeof(AngleSerializer))]
    public sealed class AngleSerializerTest : SerializationTest
    {
        [Test]
        public void SerializationTest()
        {
            var degrees = 75d;
            var angle = Angle.FromDegrees(degrees);
            var node = Serialization.WriteValueAs<ValueDataNode>(angle);
            var serializedValue = $"{MathHelper.DegreesToRadians(degrees).ToString(CultureInfo.InvariantCulture)} rad";

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
