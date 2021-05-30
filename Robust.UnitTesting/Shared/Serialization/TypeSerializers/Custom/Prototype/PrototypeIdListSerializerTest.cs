using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using YamlDotNet.RepresentationModel;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers.Custom.Prototype
{
    [TestFixture]
    [TestOf(typeof(PrototypeIdListSerializer<>))]
    public class PrototypeIdListSerializerTest : SerializationTest
    {
        private static readonly string TestEntityId = $"{nameof(PrototypeIdListSerializerTest)}Dummy";

        private static readonly string TestInvalidEntityId = $"{nameof(PrototypeIdListSerializerTest)}DummyInvalid";

        private static readonly string Prototypes = $@"
- type: entity
  id: {TestEntityId}";

        private static readonly string DataString = $@"
entitiesList:
- {TestEntityId}
entitiesReadOnlyList:
- {TestEntityId}
entitiesReadOnlyCollection:
- {TestEntityId}
entitiesImmutableList:
- {TestEntityId}";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            IoCManager.Resolve<IPrototypeManager>().LoadString(Prototypes);
        }

        [Test]
        public void SerializationTest()
        {
            var definition = new PrototypeIdListSerializerTestDataDefinition
            {
                EntitiesList = {TestEntityId},
                EntitiesReadOnlyList = new List<string>() {TestEntityId},
                EntitiesReadOnlyCollection = new List<string>() {TestEntityId},
                EntitiesImmutableList = ImmutableList.Create(TestEntityId)
            };
            var node = Serialization.WriteValueAs<MappingDataNode>(definition);

            Assert.That(node.Children.Count, Is.EqualTo(4));

            var entities = node.Cast<SequenceDataNode>("entitiesList");
            Assert.That(entities.Sequence.Count, Is.EqualTo(1));
            Assert.That(entities.Cast<ValueDataNode>(0).Value, Is.EqualTo(TestEntityId));

            var entitiesReadOnlyList = node.Cast<SequenceDataNode>("entitiesReadOnlyList");
            Assert.That(entitiesReadOnlyList.Sequence.Count, Is.EqualTo(1));
            Assert.That(entitiesReadOnlyList.Cast<ValueDataNode>(0).Value, Is.EqualTo(TestEntityId));

            var entitiesReadOnlyCollection = node.Cast<SequenceDataNode>("entitiesReadOnlyCollection");
            Assert.That(entitiesReadOnlyCollection.Sequence.Count, Is.EqualTo(1));
            Assert.That(entitiesReadOnlyCollection.Cast<ValueDataNode>(0).Value, Is.EqualTo(TestEntityId));

            var entitiesImmutableList = node.Cast<SequenceDataNode>("entitiesImmutableList");
            Assert.That(entitiesImmutableList.Sequence.Count, Is.EqualTo(1));
            Assert.That(entitiesImmutableList.Cast<ValueDataNode>(0).Value, Is.EqualTo(TestEntityId));
        }

        [Test]
        public void DeserializationTest()
        {
            var stream = new YamlStream();
            stream.Load(new StringReader(DataString));

            var node = stream.Documents[0].RootNode.ToDataNode();
            var definition = Serialization.ReadValue<PrototypeIdListSerializerTestDataDefinition>(node);

            Assert.NotNull(definition);

            Assert.That(definition!.EntitiesList.Count, Is.EqualTo(1));
            Assert.That(definition.EntitiesList[0], Is.EqualTo(TestEntityId));

            Assert.That(definition!.EntitiesReadOnlyList.Count, Is.EqualTo(1));
            Assert.That(definition.EntitiesReadOnlyList[0], Is.EqualTo(TestEntityId));

            Assert.That(definition!.EntitiesReadOnlyCollection.Count, Is.EqualTo(1));
            Assert.That(definition.EntitiesReadOnlyCollection.Single(), Is.EqualTo(TestEntityId));

            Assert.That(definition!.EntitiesImmutableList.Count, Is.EqualTo(1));
            Assert.That(definition.EntitiesImmutableList[0], Is.EqualTo(TestEntityId));
        }

        [Test]
        public void ValidationValidTest()
        {
            var validSequence = new SequenceDataNode(TestEntityId);

            var validations = Serialization.ValidateNodeWith<
                List<string>,
                PrototypeIdListSerializer<EntityPrototype>,
                SequenceDataNode>(validSequence);
            Assert.True(validations.Valid);

            validations = Serialization.ValidateNodeWith<
                IReadOnlyList<string>,
                PrototypeIdListSerializer<EntityPrototype>,
                SequenceDataNode>(validSequence);
            Assert.True(validations.Valid);

            validations = Serialization.ValidateNodeWith<
                IReadOnlyCollection<string>,
                PrototypeIdListSerializer<EntityPrototype>,
                SequenceDataNode>(validSequence);
            Assert.True(validations.Valid);

            validations = Serialization.ValidateNodeWith<
                ImmutableList<string>,
                PrototypeIdListSerializer<EntityPrototype>,
                SequenceDataNode>(validSequence);
            Assert.True(validations.Valid);
        }

        [Test]
        public void ValidationInvalidTest()
        {
            var invalidSequence = new SequenceDataNode(TestInvalidEntityId);

            var validations = Serialization.ValidateNodeWith<
                List<string>,
                PrototypeIdListSerializer<EntityPrototype>,
                SequenceDataNode>(invalidSequence);
            Assert.False(validations.Valid);

            validations = Serialization.ValidateNodeWith<
                IReadOnlyList<string>,
                PrototypeIdListSerializer<EntityPrototype>,
                SequenceDataNode>(invalidSequence);
            Assert.False(validations.Valid);

            validations = Serialization.ValidateNodeWith<
                IReadOnlyCollection<string>,
                PrototypeIdListSerializer<EntityPrototype>,
                SequenceDataNode>(invalidSequence);
            Assert.False(validations.Valid);

            validations = Serialization.ValidateNodeWith<
                ImmutableList<string>,
                PrototypeIdListSerializer<EntityPrototype>,
                SequenceDataNode>(invalidSequence);
            Assert.False(validations.Valid);
        }
    }

    [DataDefinition]
    public class PrototypeIdListSerializerTestDataDefinition
    {
        [DataField("entitiesList", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
        public List<string> EntitiesList = new();

        [DataField("entitiesReadOnlyList", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
        public IReadOnlyList<string> EntitiesReadOnlyList = new List<string>();

        [DataField("entitiesReadOnlyCollection", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
        public IReadOnlyCollection<string> EntitiesReadOnlyCollection = new List<string>();

        [DataField("entitiesImmutableList", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
        public ImmutableList<string> EntitiesImmutableList = ImmutableList<string>.Empty;
    }
}
