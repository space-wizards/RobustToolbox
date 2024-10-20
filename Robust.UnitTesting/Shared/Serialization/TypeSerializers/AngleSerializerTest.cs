using System;
using System.Globalization;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers
{
    [TestFixture]
    [TestOf(typeof(AngleSerializer))]
    public sealed class AngleSerializerTest : SerializationTest
    {
        private static readonly TestCaseData[] _source = new[]
        {
            new TestCaseData(Math.PI),
            new TestCaseData(Math.PI / 2),
            new TestCaseData(Math.PI / 4),
            new TestCaseData(0.515),
            new TestCaseData(75),
        };

        [Test, TestCaseSource(nameof(_source))]
        public void SerializationRadsTest(double radians)
        {
            var angle = new Angle(radians);
            var node = Serialization.WriteValueAs<ValueDataNode>(angle);
            var serializedValue = $"{radians.ToString(CultureInfo.InvariantCulture)} rad";

            Assert.That(node.Value, Is.EqualTo(serializedValue));
        }

        [Test, TestCaseSource(nameof(_source))]
        public void DeserializationRadsTest(double radians)
        {
            var angle = new Angle(radians);
            var node = new ValueDataNode($"{radians.ToString(CultureInfo.InvariantCulture)} rad");
            var deserializedAngle = Serialization.Read<Angle>(node);

            Assert.That(deserializedAngle, Is.EqualTo(angle));
        }

        /*
         * Serialization of degrees test won't work because it's comparing degrees to radians.
         */

        [Test, TestCaseSource(nameof(_source))]
        public void DeserializationDegreesTest(double radians)
        {
            var degrees = MathHelper.RadiansToDegrees(radians);
            var angle = Angle.FromDegrees(degrees);
            var node = new ValueDataNode($"{degrees.ToString(CultureInfo.InvariantCulture)}");
            var deserializedAngle = Serialization.Read<Angle>(node);

            Assert.That(deserializedAngle, Is.EqualTo(angle));
        }
    }
}
