﻿using System.Collections.Immutable;
using System.IO;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.Serialization.YamlObjectSerializerTests
{
    [TestFixture]
    public sealed class ImmutableListSerializationTest : RobustUnitTest
    {
        [OneTimeSetUp]
        public void Setup()
        {
            IoCManager.Resolve<ISerializationManager>().Initialize();
        }

        [Test]
        public void SerializeListTest()
        {
            // Arrange
            var data = _serializableList;
            var serMan = IoCManager.Resolve<ISerializationManager>();
            var sequence = (SequenceDataNode) serMan.WriteValue(data, notNullableOverride: true);
            var mapping = new MappingDataNode();
            mapping.Add("datalist", sequence);

            // Assert
            var result = NodeToYamlText(mapping);
            Assert.That(result, Is.EqualTo(_serializedListYaml));
        }

        [Test]
        public void DeserializeListTest()
        {
            // Arrange
            var serMan = IoCManager.Resolve<ISerializationManager>();
            var rootNode = YamlTextToNode(_serializedListYaml);

            // Act
            var data = serMan.Read<ImmutableList<int>>(new SequenceDataNode((YamlSequenceNode)rootNode.Children[0].Value), notNullableOverride: true);

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

        // serializes a node tree into text
        internal static string NodeToYamlText(DataNode root)
        {
            var document = new YamlDocument(root.ToYamlNode());

            using var writer = new StringWriter();

            var yamlStream = new YamlStream(document);
            yamlStream.Save(new Emitter(writer, new EmitterSettings().WithNewLine("\n")), true);
            writer.Flush();
            return writer.ToString();
        }

        // deserializes yaml text, loads the first document, and returns the first entity
        internal static YamlMappingNode YamlTextToNode(string text)
        {
            using var stream = new MemoryStream();

            // create a stream for testing
            var writer = new StreamWriter(stream);
            writer.Write(text);
            writer.Flush();
            stream.Position = 0;

            // deserialize stream
            var reader = new StreamReader(stream);
            var yamlStream = new YamlStream();
            yamlStream.Load(reader);

            // read first document
            var firstDoc = yamlStream.Documents[0];

            return (YamlMappingNode)firstDoc.RootNode;
        }
    }
}
