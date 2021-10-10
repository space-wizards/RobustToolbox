using System;
using NUnit.Framework;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers.Custom
{
    [TestFixture]
    [TestOf(typeof(FlagSerializer<>))]
    public class FlagSerializerTest : SerializationTest
    {
        [Test]
        public void SingleFlagTest()
        {
            var definition = new TestDefinition {Flag = (int) TestFlagsEnum.One};

            var node = Serialization.WriteValueAs<MappingDataNode>(definition);
            Assert.That(node.Children.Count, Is.EqualTo(1));

            var sequence = node.Cast<SequenceDataNode>("flag");
            Assert.That(sequence.Sequence.Count, Is.EqualTo(1));
            Assert.That(sequence.Cast<ValueDataNode>(0).Value, Is.EqualTo("One"));

            var value = Serialization.ReadValueOrThrow<TestDefinition>(node);
            Assert.That(value.Flag, Is.EqualTo(1));
        }

        [Test]
        public void DualFlagTest()
        {
            var definition = new TestDefinition {Flag = (int) TestFlagsEnum.Three};

            var node = Serialization.WriteValueAs<MappingDataNode>(definition);
            Assert.That(node.Children.Count, Is.EqualTo(1));

            var sequence = node.Cast<SequenceDataNode>("flag");
            Assert.That(sequence.Sequence.Count, Is.EqualTo(2));
            Assert.That(sequence.Sequence, Does.Contain(new ValueDataNode("One")));
            Assert.That(sequence.Sequence, Does.Contain(new ValueDataNode("Two")));

            var value = Serialization.ReadValueOrThrow<TestDefinition>(node);
            Assert.That(value.Flag, Is.EqualTo(3));
        }

        [Test]
        public void NegativeFlagTest()
        {
            var definition = new TestDefinition {Flag = (int) TestFlagsEnum.NegativeFlag};

            var node = Serialization.WriteValueAs<MappingDataNode>(definition);
            Assert.That(node.Children.Count, Is.EqualTo(1));

            var sequence = node.Cast<SequenceDataNode>("flag");
            Assert.That(sequence.Sequence.Count, Is.EqualTo(1));
            Assert.That(sequence.Cast<ValueDataNode>(0).Value, Is.EqualTo("NegativeFlag"));

            var value = Serialization.ReadValueOrThrow<TestDefinition>(node);
            Assert.That(value.Flag, Is.EqualTo(TestFlags.Negative));
        }

        private class TestFlags
        {
            public const int Negative = 1 << 31;
        }

        [Flags, FlagsFor(typeof(TestFlags))]
        private enum TestFlagsEnum
        {
            Default = 0,
            One = 1 << 0,
            Two = 1 << 1,
            Three = One | Two,
            // NotDefined = 1 << 2, // 4
            NegativeFlag = TestFlags.Negative
        }

        [DataDefinition]
        private class TestDefinition
        {
            [DataField("flag", customTypeSerializer: typeof(FlagSerializer<TestFlags>))]
            public int Flag { get; set; }
        }
    }
}
