using NUnit.Framework;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Robust.UnitTesting.Shared.Serialization
{
    [TestFixture]
    [TestOf(typeof(YamlConstantSerializer))]
    class YamlConstantSerializer_Test : RobustUnitTest
    {
        public sealed class GenericConstantTag {}

        [Test]
        public void SerializeOneConstantTest()
        {
            // Arrange
            var data = (int)GenericConstants.One;
            var mapping = new YamlMappingNode();
            var serializer = YamlObjectSerializer.NewWriter(mapping);

            // Act
            serializer.DataField(ref data, "generic_constants", 0, WithFormat.Constants<GenericConstantTag>());

            // Assert
            var result = YamlObjectSerializer_Test.NodeToYamlText(mapping);
            Assert.That(result, Is.EqualTo(SerializedOneConstant));
        }

        [Test]
        public void DeserializeOneConstantTest()
        {
            // Arrange
            var data = 0;
            var rootNode = YamlObjectSerializer_Test.YamlTextToNode(SerializedOneConstant);
            var serializer = YamlObjectSerializer.NewReader(rootNode);

            // Act
            serializer.DataField(ref data, "generic_constants", 0, WithFormat.Constants<GenericConstantTag>());

            // Assert
            Assert.That(data, Is.EqualTo((int)GenericConstants.One));
        }

        [Test]
        public void DeserializeLegacyFormatTest()
        {
            // Arrange
            var data = 0;
            var rootNode = YamlObjectSerializer_Test.YamlTextToNode(SerializedThree);
            var serializer = YamlObjectSerializer.NewReader(rootNode);

            // Act
            serializer.DataField(ref data, "generic_constants", 0, WithFormat.Constants<GenericConstantTag>());

            // Assert
            Assert.That(data, Is.EqualTo((int)GenericConstants.Three));
        }

        private const string SerializedOneConstant = "generic_constants: One\n...\n";
        private const string SerializedThree = "generic_constants: 3\n...\n";

        [ConstantsFor(typeof(GenericConstantTag))]
        private enum GenericConstants
        {
            One = 1,
            Three = 3,
        }
    }
}
