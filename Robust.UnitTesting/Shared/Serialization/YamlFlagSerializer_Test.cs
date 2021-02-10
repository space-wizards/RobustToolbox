using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.UnitTesting.Shared.Serialization.YamlObjectSerializerTests;
using YamlDotNet.RepresentationModel;

namespace Robust.UnitTesting.Shared.Serialization
{
    [TestFixture]
    [TestOf(typeof(YamlFlagSerializer))]
    class YamlFlagSerializer_Test : RobustUnitTest
    {
        public sealed class GenericFlagTag {}
        public sealed class GenericFlagWithZeroTag {}

        [Test]
        public void SerializeOneFlagTest()
        {
            // Arrange
            var data = (int)GenericFlags.One;
            var mapping = new YamlMappingNode();
            var serializer = YamlObjectSerializer.NewWriter(mapping);

            // Act
            serializer.DataField(ref data, "generic_flags", 0, WithFormat.Flags<GenericFlagTag>());

            // Assert
            var result = YamlObjectSerializer_Test.NodeToYamlText(mapping);
            Assert.That(result, Is.EqualTo(SerializedOneFlag));
        }

        [Test]
        public void DeserializeOneFlagTest()
        {
            // Arrange
            var data = 0;
            var rootNode = YamlObjectSerializer_Test.YamlTextToNode(SerializedOneFlag);
            var serializer = YamlObjectSerializer.NewReader(rootNode);

            // Act
            serializer.DataField(ref data, "generic_flags", 0, WithFormat.Flags<GenericFlagTag>());

            // Assert
            Assert.That(data, Is.EqualTo((int)GenericFlags.One));
        }

        [Test]
        public void SerializeFiveFlagTest()
        {
            // Arrange
            var data = (int)GenericFlags.Five;
            var mapping = new YamlMappingNode();
            var serializer = YamlObjectSerializer.NewWriter(mapping);

            // Act
            serializer.DataField(ref data, "generic_flags", 0, WithFormat.Flags<GenericFlagTag>());

            // Assert
            var result = YamlObjectSerializer_Test.NodeToYamlText(mapping);
            Assert.That(result, Is.EqualTo(SerializedOneFourFlag));
        }

        [Test]
        public void DeserializeFiveFlagTest()
        {
            // Arrange
            var data = 0;
            var rootNode = YamlObjectSerializer_Test.YamlTextToNode(SerializedFiveFlag);
            var serializer = YamlObjectSerializer.NewReader(rootNode);

            // Act
            serializer.DataField(ref data, "generic_flags", 0, WithFormat.Flags<GenericFlagTag>());

            // Assert
            Assert.That(data, Is.EqualTo((int)GenericFlags.Five));
        }

        [Test]
        public void SerializeZeroWithoutFlagTest()
        {
            // Arrange
            var data = 0;
            var mapping = new YamlMappingNode();
            var serializer = YamlObjectSerializer.NewWriter(mapping);

            // Act
            serializer.DataField(ref data, "generic_flags", 0, WithFormat.Flags<GenericFlagTag>(), alwaysWrite: true);

            // Assert
            var result = YamlObjectSerializer_Test.NodeToYamlText(mapping);
            Assert.That(result, Is.EqualTo(SerializedZeroNum));
        }

        [Test]
        public void DeserializeZeroWithoutFlagTest()
        {
            // Arrange
            var data = 0;
            var rootNode = YamlObjectSerializer_Test.YamlTextToNode(SerializedZeroNum);
            var serializer = YamlObjectSerializer.NewReader(rootNode);

            // Act
            serializer.DataField(ref data, "generic_flags", 0, WithFormat.Flags<GenericFlagTag>());

            // Assert
            Assert.That(data, Is.EqualTo(0));
        }

        private const string SerializedOneFlag = "generic_flags:\n- One\n...\n";
        private const string SerializedOneFourFlag = "generic_flags:\n- One\n- Four\n...\n";
        private const string SerializedFiveFlag = "generic_flags:\n- Five\n...\n";
        private const string SerializedZeroNum = "generic_flags: 0\n...\n";

        [Test]
        public void SerializeZeroWithFlagTest()
        {
            // Arrange
            var data = 0;
            var mapping = new YamlMappingNode();
            var serializer = YamlObjectSerializer.NewWriter(mapping);

            // Act
            serializer.DataField(ref data, "generic_flags_with_zero", 0, WithFormat.Flags<GenericFlagWithZeroTag>(), alwaysWrite: true);

            // Assert
            var result = YamlObjectSerializer_Test.NodeToYamlText(mapping);
            Assert.That(result, Is.EqualTo(SerializedZeroFlag));
        }

        [Test]
        public void DeserializeZeroWithFlagTest()
        {
            // Arrange
            var data = 0;
            var rootNode = YamlObjectSerializer_Test.YamlTextToNode(SerializedZeroFlag);
            var serializer = YamlObjectSerializer.NewReader(rootNode);

            // Act
            serializer.DataField(ref data, "generic_flags_with_zero", 0, WithFormat.Flags<GenericFlagWithZeroTag>());

            // Assert
            Assert.That(data, Is.EqualTo(0));
        }

        [Test]
        public void SerializeNonZeroWithZeroFlagDoesntShowZeroTest()
        {
            // Arrange
            var data = (int)FlagsWithZero.Two;
            var mapping = new YamlMappingNode();
            var serializer = YamlObjectSerializer.NewWriter(mapping);

            // Act
            serializer.DataField(ref data, "generic_flags_with_zero", 0, WithFormat.Flags<GenericFlagWithZeroTag>());

            // Assert
            var result = YamlObjectSerializer_Test.NodeToYamlText(mapping);
            Assert.That(result, Is.EqualTo(SerializedTwoWithZeroFlag));
        }

        private const string SerializedZeroFlag = "generic_flags_with_zero:\n- None\n...\n";
        private const string SerializedTwoWithZeroFlag = "generic_flags_with_zero:\n- Two\n...\n";

        [Flags]
        [FlagsFor(typeof(GenericFlagTag))]
        private enum GenericFlags
        {
            One = 1,
            Two = 2,
            Four = 4,
            Five = 5,
        }

        [Flags]
        [FlagsFor(typeof(GenericFlagWithZeroTag))]
        private enum FlagsWithZero
        {
            None = 0,
            One = 1,
            Two = 2,
            Four = 4,
            Five = 5,
        }
    }
}
