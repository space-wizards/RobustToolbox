﻿using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers
{
    [TestFixture]
    [TestOf(typeof(ColorSerializer))]
    public sealed class ColorSerializerTest : SerializationTest
    {
        [Test]
        public void SerializationTest()
        {
            byte r = 123;
            byte g = 5;
            byte b = 40;
            byte a = 55;
            var color = new Color(r, g, b, a);
            var node = Serialization.WriteValueAs<ValueDataNode>(color);
            var str = $"#{(r << 24) + (g << 16) + (b << 8) + a:X8}";

            Assert.That(node.Value, Is.EqualTo(str));
        }

        [Test]
        public void DeserializationTest()
        {
            byte r = 123;
            byte g = 5;
            byte b = 40;
            byte a = 55;
            var str = $"#{(r << 24) + (g << 16) + (b << 8) + a:X8}";
            var node = new ValueDataNode(str);
            var deserializedColor = Serialization.Read<Color>(node);
            var color = new Color(r, g, b, a);

            Assert.That(deserializedColor.ToString(), Is.EqualTo(color.ToString()));
        }

        [Test]
        public void DeserializeNullableNullTest()
        {
            var node = ValueDataNode.Null();
            var color = Serialization.Read<Color?>(node);

            Assert.That(color, Is.Null);
        }

        [Test]
        public void DeserializeNullableNotNullTest()
        {
            var node = new ValueDataNode("#FFFFFFFF");
            var color = Serialization.Read<Color?>(node);

            Assert.That(color, Is.Not.Null);
            Assert.That(color, Is.EqualTo(Color.White));
        }

        [Test]
        public void DeserializeNullTest()
        {
            var node = ValueDataNode.Null();
            Assert.That(() => Serialization.Read<Color>(node), Throws.Exception);
        }

        [Test]
        public void DeserializeNotNullTest()
        {
            var node = new ValueDataNode("#FFFFFFFF");
            var color = Serialization.Read<Color>(node);

            Assert.That(color, Is.EqualTo(Color.White));
        }
    }
}
