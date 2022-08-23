using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers
{
    [TestFixture]
    [TestOf(typeof(Box2Serializer))]
    public sealed class Box2SerializerTest : SerializationTest
    {
        [Test]
        public void SerializationTest()
        {
            var left = 1;
            var bottom = -2;
            var right = -3;
            var top = 4;
            var str = $"{left},{bottom},{right},{top}";
            var box = new Box2(left, bottom, right, top);
            var node = Serialization.WriteValueAs<ValueDataNode>(box);

            Assert.That(node.Value, Is.EqualTo(str));
        }

        [Test]
        public void DeserializationTest()
        {
            var left = 1;
            var bottom = -2;
            var right = -3;
            var top = 4;
            var str = $"{left},{bottom},{right},{top}";
            var node = new ValueDataNode(str);
            var deserializedBox = Serialization.Read<Box2>(node);
            var box = new Box2(left, bottom, right, top);

            Assert.That(deserializedBox, Is.EqualTo(box));

            Assert.That(deserializedBox.Left, Is.EqualTo(box.Left));
            Assert.That(deserializedBox.Bottom, Is.EqualTo(box.Bottom));
            Assert.That(deserializedBox.Right, Is.EqualTo(box.Right));
            Assert.That(deserializedBox.Top, Is.EqualTo(box.Top));

            Assert.That(deserializedBox.BottomLeft, Is.EqualTo(box.BottomLeft));
            Assert.That(deserializedBox.BottomRight, Is.EqualTo(box.BottomRight));
            Assert.That(deserializedBox.TopLeft, Is.EqualTo(box.TopLeft));
            Assert.That(deserializedBox.TopRight, Is.EqualTo(box.TopRight));
        }
    }
}
