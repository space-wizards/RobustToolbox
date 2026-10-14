using NUnit.Framework;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.UnitTesting.Shared.Serialization;

namespace Robust.Shared.IntegrationTests.Serialization;

public sealed partial class AlwaysPushInheritanceTest : OurSerializationTest
{
    [Test]
    public void ReadWriteOrderPreservedTest()
    {
        var listTag = DataDefinitionUtility.AutoGenerateTag(nameof(AlwaysPushInheritanceTestDefinition.List));
        var parent = new MappingDataNode
        {
            [IdDataFieldAttribute.Name] = new ValueDataNode("parent"),
            [listTag] = new SequenceDataNode("0", "1", "2")
        };

        var child = new MappingDataNode
        {
            [ParentDataFieldAttribute.Name] = new ValueDataNode("parent"),
            [IdDataFieldAttribute.Name] = new ValueDataNode("child"),
            [listTag] = new SequenceDataNode("3", "4", "5")
        };

        var composed = Serialization.PushComposition<AlwaysPushInheritanceTestDefinition, MappingDataNode>(parent, child);

        // The composed node has the same nodes as the child, plus the list elements of the parent
        Assert.That(composed, Does.ContainKey(ParentDataFieldAttribute.Name));
        Assert.That(composed, Does.ContainKey(IdDataFieldAttribute.Name));
        Assert.That(composed, Does.ContainKey(listTag));
        Assert.That(composed, Has.Count.EqualTo(3));

        var sequence = composed[listTag] as SequenceDataNode;
        Assert.That(sequence, Is.Not.Null);
        Assert.That(sequence, Has.Count.EqualTo(6));

        // The composed list has all the elements from the parent plus the child
        for (var i = 0; i < 6; i++)
        {
            Assert.That(sequence.Sequence[i], Is.TypeOf<ValueDataNode>());

            var value = (ValueDataNode)sequence.Sequence[i];
            Assert.That(value.Value, Is.EqualTo($"{i}"));
        }

        var read = Serialization.Read<AlwaysPushInheritanceTestDefinition>(composed, notNullableOverride: true);

        // The read definition has the same list elements as the composed node
        Assert.That(read.List, Has.Count.EqualTo(6));
        for (var i = 0; i < 6; i++)
        {
            Assert.That(read.List[i], Is.EqualTo(i));
        }

        var write = (MappingDataNode) Serialization.WriteValue(read, notNullableOverride: true);

        // The re-written mapping has a list equivalent to the one from the original composed mapping
        Assert.That(write, Does.ContainKey(listTag));
        Assert.That(write[listTag], Is.EquivalentTo((SequenceDataNode) composed[listTag]));

        var read2 = Serialization.Read<AlwaysPushInheritanceTestDefinition>(write, notNullableOverride: true);

        // The re-read definition has a list equivalent to the one from the original definition used to re-write the mapping node
        Assert.That(read.List, Is.EquivalentTo(read2.List));
    }

    [DataDefinition]
    private sealed partial class AlwaysPushInheritanceTestDefinition
    {
        [DataField]
        [AlwaysPushInheritance]
        public List<int> List = new();
    }
}
