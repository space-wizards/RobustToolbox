using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.UnitTesting.Shared.Serialization
{
    [Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
    [TestFixture]
    [TestOf(typeof(YamlObjectSerializer))]
    class YamlObjectSerializer_Test
    {
        [Test]
        public void SerializeListTest()
        {
            // Arrange
            var data = SerializableList;
            var mapping = new YamlMappingNode();
            var serializer = YamlObjectSerializer.NewWriter(mapping);

            // Act
            serializer.DataField(ref data, "datalist", new List<int>(0));

            // Assert
            var result = NodeToYamlText(mapping);
            Assert.That(result, Is.EqualTo(SerializedListYaml));
        }

        [Test]
        public void DeserializeListTest()
        {
            // Arrange
            List<int> data = null;
            var rootNode = YamlTextToNode(SerializedListYaml);
            var serializer = YamlObjectSerializer.NewReader(rootNode);

            // Act
            serializer.DataField(ref data, "datalist", new List<int>(0));

            // Assert
            Assert.That(data, Is.Not.Null);
            Assert.That(data.Count, Is.EqualTo(SerializableList.Count));
            for (var i = 0; i < SerializableList.Count; i++)
                Assert.That(data[i], Is.EqualTo(SerializableList[i]));
        }

        private readonly string SerializedListYaml = "datalist:\n- 1\n- 2\n- 3\n...\n";
        private readonly List<int> SerializableList = new List<int> { 1, 2, 3 };

        [Test]
        public void SerializeDictTest()
        {
            // Arrange
            var data = SerializableDict;
            var mapping = new YamlMappingNode();
            var serializer = YamlObjectSerializer.NewWriter(mapping);

            // Act
            serializer.DataField(ref data, "datadict", new Dictionary<string, int>(0));

            // Assert
            var result = NodeToYamlText(mapping);
            Assert.That(result, Is.EqualTo(SerializedDictYaml));
        }

        [Test]
        public void DeserializeDictTest()
        {
            Dictionary<string, int> data = null;
            var rootNode = YamlTextToNode(SerializedDictYaml);
            var serializer = YamlObjectSerializer.NewReader(rootNode);

            // Act
            serializer.DataField(ref data, "datadict", new Dictionary<string, int>(0));

            // Assert
            Assert.That(data, Is.Not.Null);
            Assert.That(data.Count, Is.EqualTo(SerializableDict.Count));
            foreach (var kvEntry in SerializableDict)
                Assert.That(data[kvEntry.Key], Is.EqualTo(kvEntry.Value));
        }

        [Test]
        public void DeserializeExpressionTest()
        {
            var dummy = new DummyClass();

            var rootNode = YamlTextToNode("foo: 5\nbar: \"baz\"");
            var serializer = YamlObjectSerializer.NewReader(rootNode);

            serializer.DataField(dummy, d => d.Foo, "foo", 4);
            serializer.DataField(dummy, d => d.Bar, "bar", "honk");
            serializer.DataField(dummy, d => d.Baz, "baz", Color.Black);

            Assert.That(dummy.Foo, Is.EqualTo(5));
            Assert.That(dummy.Bar, Is.EqualTo("baz"));
            Assert.That(dummy.Baz, Is.EqualTo(Color.Black));
        }

        [Test]
        public void SerializeExpressionTest()
        {
            var dummy = new DummyClass
            {
                Bar = "honk!",
                Baz = Color.Black,
                Foo = 5
            };

            var mapping = new YamlMappingNode();
            var serializer = YamlObjectSerializer.NewWriter(mapping);

            serializer.DataField(dummy, d => d.Foo, "foo", 1);
            serializer.DataField(dummy, d => d.Bar, "bar", "*silence*");
            serializer.DataField(dummy, d => d.Baz, "baz", Color.Black);

            Assert.That(mapping, Is.EquivalentTo(new YamlMappingNode {{"bar", "honk!"}, {"foo", "5"}}));
        }

        private readonly string SerializedDictYaml = "datadict:\n  val1: 1\n  val2: 2\n...\n";
        private readonly Dictionary<string, int> SerializableDict = new Dictionary<string, int> { { "val1", 1 }, { "val2", 2 } };

        // serializes a node tree into text
        internal static string NodeToYamlText(YamlNode root)
        {
            var document = new YamlDocument(root);

            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.NewLine = "\n";
                    var yamlStream = new YamlStream(document);
                    yamlStream.Save(writer);
                    writer.Flush();
                    return EncodingHelpers.UTF8.GetString(stream.ToArray());
                }
            }
        }

        // deserializes yaml text, loads the first document, and returns the first entity
        internal static YamlMappingNode YamlTextToNode(string text)
        {
            using (var stream = new MemoryStream())
            {
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

        private class DummyClass
        {
            public int Foo { get; set; }
            public string Bar { get; set; }
            public Color Baz { get; set; } = Color.Orange;
        }
    }
}
