using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;
// ReSharper disable AccessToStaticMemberViaDerivedType

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
        public void SerializeListAsReadOnlyCollectionTest()
        {
            // Arrange
            IReadOnlyCollection<int> data = SerializableList;
            var mapping = new YamlMappingNode();
            var serializer = YamlObjectSerializer.NewWriter(mapping);

            // Act
            serializer.DataField(ref data, "datalist", Array.Empty<int>());

            // Assert
            var result = NodeToYamlText(mapping);
            Assert.That(result, Is.EqualTo(SerializedListYaml));
        }

        [Test]
        public void SerializeListAsReadOnlyListTest()
        {
            // Arrange
            IReadOnlyList<int> data = SerializableList;
            var mapping = new YamlMappingNode();
            var serializer = YamlObjectSerializer.NewWriter(mapping);

            // Act
            serializer.DataField(ref data, "datalist", Array.Empty<int>());

            // Assert
            var result = NodeToYamlText(mapping);
            Assert.That(result, Is.EqualTo(SerializedListYaml));
        }

        [Test]
        public void DeserializeListTest()
        {
            // Arrange
            List<int> data = null!;
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

        [Test]
        public void DeserializeListAsReadOnlyListTest()
        {
            // Arrange
            IReadOnlyList<int> data = null!;
            var rootNode = YamlTextToNode(SerializedListYaml);
            var serializer = YamlObjectSerializer.NewReader(rootNode);

            // Act
            serializer.DataField(ref data, "datalist", Array.Empty<int>());

            // Assert
            Assert.That(data, Is.Not.Null);
            Assert.That(data.Count, Is.EqualTo(SerializableList.Count));
            for (var i = 0; i < SerializableList.Count; i++)
                Assert.That(data[i], Is.EqualTo(SerializableList[i]));
        }

        [Test]
        public void DeserializeListAsReadOnlyCollectionTest()
        {
            // Arrange
            IReadOnlyCollection<int> data = null!;
            var rootNode = YamlTextToNode(SerializedListYaml);
            var serializer = YamlObjectSerializer.NewReader(rootNode);

            // Act
            serializer.DataField(ref data, "datalist", Array.Empty<int>());

            // Assert
            Assert.That(data, Is.EquivalentTo(SerializableList));
        }


        private readonly string SerializedListYaml = "datalist:\n- 1\n- 2\n- 3\n...\n";
        private readonly List<int> SerializableList = new() { 1, 2, 3 };

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
        public void SerializeDictAsReadOnlyTest()
        {
            // Arrange
            IReadOnlyDictionary<string, int> data = SerializableDict;
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
            Dictionary<string, int> data = null!;
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
        public void DeserializeDictToReadOnlyTest()
        {
            IReadOnlyDictionary<string, int> data = null!;
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
            serializer.CurrentType = typeof(DummyClass);

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
            serializer.CurrentType = typeof(DummyClass);

            serializer.DataField(dummy, d => d.Foo, "foo", 1);
            serializer.DataField(dummy, d => d.Bar, "bar", "*silence*");
            serializer.DataField(dummy, d => d.Baz, "baz", Color.Black);

            Assert.That(mapping, Is.EquivalentTo(new YamlMappingNode {{"bar", "honk!"}, {"foo", "5"}}));
        }

        [Test]
        public void SerializedEqualDictTest()
        {
            var dict = new Dictionary<string, string>
            {
                ["A"] = "B",
                ["C"] = "W",
                ["D"] = "G",
                ["E"] = "J"
            };

            var dict2 = new Dictionary<string, string>(dict);

            Assert.That(YamlObjectSerializer.IsSerializedEqual(dict, dict2), Is.True);
        }

        [Test]
        public void TestSelfSerialize()
        {
            var mapping = new YamlMappingNode();
            var reader = YamlObjectSerializer.NewWriter(mapping);

            var field = new SelfSerializeTest {Value = 456};

            reader.DataField(ref field, "foo", default);

            Assert.That(mapping["foo"].AsString(), Is.EqualTo("456"));
        }

        [Test]
        public void TestSelfDeserialize()
        {
            var dict = new YamlMappingNode
            {
                {"foo", "123"}
            };

            var reader = YamlObjectSerializer.NewReader(dict);

            SelfSerializeTest field = default;

            reader.DataField(ref field, "foo", default);

            Assert.That(field.Value, Is.EqualTo(123));
        }

        private readonly string SerializedDictYaml = "datadict:\n  val1: 1\n  val2: 2\n...\n";
        private readonly Dictionary<string, int> SerializableDict = new() { { "val1", 1 }, { "val2", 2 } };

        [Test]
        public void SerializeHashSetTest()
        {
            var mapping = new YamlMappingNode();
            var set = SerializableSet;

            var writer = YamlObjectSerializer.NewWriter(mapping);

            writer.DataField(ref set, "dataSet", new HashSet<int>(0));

            var result = NodeToYamlText(mapping);
            Assert.That(result, Is.EqualTo(SerializedSetYaml));
        }

        [Test]
        public void DeserializeHashSetTest()
        {
            HashSet<int> data = null!;
            var rootNode = YamlTextToNode(SerializedSetYaml);
            var serializer = YamlObjectSerializer.NewReader(rootNode);

            serializer.DataField(ref data, "dataSet", new HashSet<int>(0));

            Assert.That(data, Is.Not.Null);
            Assert.That(data.Count, Is.EqualTo(SerializableSet.Count));
            for (var i = 0; i < SerializableSet.Count; i++)
                Assert.That(data.ElementAt(i), Is.EqualTo(SerializableSet.ElementAt(i)));
        }

        [Test]
        public void SerializedEqualHashSetTest()
        {
            var set = new HashSet<string> {"A", "B", "C", "D", "E"};
            var set2 = new HashSet<string>(set);

            Assert.That(YamlObjectSerializer.IsSerializedEqual(set, set2), Is.True);
        }

        [Test]
        public void SerializedNotEqualHashSetTest()
        {
            var set = new HashSet<string> {"A"};
            var set2 = new HashSet<string> {"B"};

            Assert.That(YamlObjectSerializer.IsSerializedEqual(set, set2), Is.False);
        }

        private readonly string SerializedSetYaml = "dataSet:\n- 1\n- 2\n- 3\n...\n";
        private readonly HashSet<int> SerializableSet = new() { 1, 2, 3 };

        [Test]
        public void SerializePairTest()
        {
            var mapping = new YamlMappingNode();
            var pair = SerializablePair;

            var writer = YamlObjectSerializer.NewWriter(mapping);

            writer.DataField(ref pair, "dataPair", new KeyValuePair<string, int>("val0", 0));

            var result = NodeToYamlText(mapping);
            Assert.That(result, Is.EqualTo(SerializedPairYaml));
        }

        [Test]
        public void DeserializePairTest()
        {
            KeyValuePair<string, int> data = default;
            var rootNode = YamlTextToNode(SerializedPairYaml);
            var serializer = YamlObjectSerializer.NewReader(rootNode);

            serializer.DataField(ref data, "dataPair", new KeyValuePair<string, int>("val0", 0));

            Assert.That(data, Is.Not.EqualTo(default(KeyValuePair<string, int>)));
            Assert.That(data.Key, Is.EqualTo(SerializablePair.Key));
            Assert.That(data.Value, Is.EqualTo(SerializablePair.Value));
        }

        [Test]
        public void SerializeDefaultPairTest()
        {
            var mapping = new YamlMappingNode();
            var pair = SerializableDefaultPair;

            var writer = YamlObjectSerializer.NewWriter(mapping);

            writer.DataField(ref pair, "dataPair", new KeyValuePair<int, int>(0, 0));

            var result = NodeToYamlText(mapping);
            Assert.That(result, Is.EqualTo(SerializedDefaultPairYaml));
        }

        [Test]
        public void DeserializeDefaultPairTest()
        {
            KeyValuePair<int, int> data = default;
            var rootNode = YamlTextToNode(SerializedDefaultPairYaml);
            var serializer = YamlObjectSerializer.NewReader(rootNode);

            serializer.DataField(ref data, "dataPair", new KeyValuePair<int, int>(0, 0));

            Assert.That(data, Is.EqualTo(default(KeyValuePair<int, int>)));
            Assert.That(data.Key, Is.EqualTo(SerializableDefaultPair.Key));
            Assert.That(data.Value, Is.EqualTo(SerializableDefaultPair.Value));
        }

        [Test]
        public void DeserializeNoPairTest()
        {
            KeyValuePair<int, int> data = default;
            var rootNode = YamlTextToNode(SerializedNoPairYaml);
            var serializer = YamlObjectSerializer.NewReader(rootNode);

            serializer.DataField(ref data, "dataPair", new KeyValuePair<int, int>(0, 0));

            Assert.That(data, Is.EqualTo(default(KeyValuePair<int, int>)));
            Assert.That(data.Key, Is.EqualTo(SerializedNoPair.Key));
            Assert.That(data.Value, Is.EqualTo(SerializedNoPair.Value));
        }

        [Test]
        public void SerializedEqualPairTest()
        {
            var pair = new KeyValuePair<string, int>("val0", 0);
            var pair2 = new KeyValuePair<string, int>("val0", 0);

            Assert.That(YamlObjectSerializer.IsSerializedEqual(pair, pair2), Is.True);
        }

        [Test]
        public void SerializedNotEqualPairTest()
        {
            var pair = new KeyValuePair<string, int>("val0", 0);
            var pair2 = new KeyValuePair<string, int>("val0", 1);

            Assert.That(YamlObjectSerializer.IsSerializedEqual(pair, pair2), Is.False);
        }

        private readonly string SerializedPairYaml = "dataPair:\n  val1: 1\n...\n";
        private readonly KeyValuePair<string, int> SerializablePair = new("val1", 1);

        private readonly string SerializedDefaultPairYaml = "{}\n...\n";
        private readonly KeyValuePair<int, int> SerializableDefaultPair = new(0, 0);

        private readonly string SerializedNoPairYaml = "dataPair: {}\n...\n";
        private readonly KeyValuePair<int, int> SerializedNoPair = new(0, 0);

        [Test]
        public void NullablePrimitiveSerializeNullTest()
        {
            var mapping = new YamlMappingNode();
            var reader = YamlObjectSerializer.NewWriter(mapping);

            int? value = null;

            reader.DataField(ref value, "foo", null);

            Assert.That(mapping.Children.Count, Is.Zero);
        }

        [Test]
        public void NullablePrimitiveSerializeValueTest()
        {
            var mapping = new YamlMappingNode();
            var reader = YamlObjectSerializer.NewWriter(mapping);

            int? value = 5;

            reader.DataField(ref value, "foo", null);

            Assert.That(mapping["foo"].AsInt(), Is.EqualTo(5));
        }

        [Test]
        public void NullablePrimitiveDeserializeNullTest()
        {
            var mapping = new YamlMappingNode
            {
                {"foo", null!}
            };
            var reader = YamlObjectSerializer.NewReader(mapping);

            int? value = null;

            reader.DataField(ref value, "foo", null);

            Assert.That(value, Is.Null);
        }

        [Test]
        public void NullablePrimitiveDeserializeEmptyTest()
        {
            var mapping = new YamlMappingNode
            {
                {"foo", ""}
            };
            var reader = YamlObjectSerializer.NewReader(mapping);

            int? value = null;

            reader.DataField(ref value, "foo", null);

            Assert.That(value, Is.Null);
        }

        [Test]
        public void NullablePrimitiveDeserializeNothingTest()
        {
            var mapping = new YamlMappingNode();
            var reader = YamlObjectSerializer.NewReader(mapping);

            int? value = null;

            reader.DataField(ref value, "foo", null);

            Assert.That(value, Is.Null);
        }

        [Test]
        public void NullablePrimitiveDeserializeValueTest()
        {
            var mapping = new YamlMappingNode
            {
                {"foo", "5"}
            };
            var reader = YamlObjectSerializer.NewReader(mapping);

            int? value = null;

            reader.DataField(ref value, "foo", null);

            Assert.That(value, Is.EqualTo(5));
        }

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
            public string Bar { get; set; } = default!;
            public Color Baz { get; set; } = Color.Orange;
        }

        private struct SelfSerializeTest : ISelfSerialize
        {
            public int Value;

            public void Deserialize(string value)
            {
                Value = int.Parse(value, CultureInfo.InvariantCulture);
            }

            public string Serialize()
            {
                return Value.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
