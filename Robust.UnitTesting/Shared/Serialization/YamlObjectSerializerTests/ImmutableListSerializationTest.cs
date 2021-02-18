using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using NUnit.Framework;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;
using static Robust.UnitTesting.Shared.Serialization.YamlObjectSerializerTests.YamlObjectSerializer_Test;
// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.Serialization.YamlObjectSerializerTests
{
    [Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
    [TestFixture]
    [TestOf(typeof(YamlObjectSerializer))]
    public class ImmutableListSerializationTest
    {
        [Test]
        public void SerializeListTest()
        {
            // Arrange
            var data = _serializableList;
            var mapping = new YamlMappingNode();
            var serializer = YamlObjectSerializer.NewWriter(mapping);

            // Act
            serializer.DataField(ref data, "datalist", ImmutableList<int>.Empty);

            // Assert
            var result = NodeToYamlText(mapping);
            Assert.That(result, Is.EqualTo(_serializedListYaml));
        }

        [Test]
        public void DeserializeListTest()
        {
            // Arrange
            ImmutableList<int> data = null!;
            var rootNode = YamlTextToNode(_serializedListYaml);
            var serializer = YamlObjectSerializer.NewReader(rootNode);

            // Act
            serializer.DataField(ref data, "datalist", ImmutableList<int>.Empty);

            // Assert
            Assert.That(data, Is.Not.Null);
            Assert.That(data.Count, Is.EqualTo(_serializableList.Count));

            for (var i = 0; i < _serializableList.Count; i++)
            {
                Assert.That(data[i], Is.EqualTo(_serializableList[i]));
            }
        }

        private readonly string _serializedListYaml = "datalist:\n- 1\n- 2\n- 3\n...\n";
        private readonly ImmutableList<int> _serializableList = ImmutableList.Create<int>(1, 2, 3);
    }
}
