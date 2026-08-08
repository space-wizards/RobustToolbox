using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;

namespace Robust.UnitTesting.Shared.Serialization;

[TestFixture]
internal sealed partial class CompositionTest : OurSerializationTest
{
    [DataDefinition]
    private sealed partial class CompositionTestClass
    {
        [DataField("f1")] public int ChildValue;
        [DataField("f2")] public int Parent1Value;
        [DataField("f3")] public int Parent2Value;
        [DataField("f4"), NeverPushInheritance]
        public int NeverPushValueParent1;
        [DataField("f5"), NeverPushInheritance]
        public int NeverPushValueParent2;
        [DataField("f6"), AlwaysPushInheritance]
        public List<int> AlwaysPushValues = [];
    }

    [Test]
    public void TestPushComposition()
    {
        var child = new MappingDataNode { { "f1", "1" } };
        var parent1 = new MappingDataNode
        {
            { "f1", "2" },
            { "f2", "1" },
            { "f4", "1" }
        };
        var parent2 = new MappingDataNode
        {
            { "f1", "3" },
            { "f2", "2" },
            { "f3", "1" },
            { "f5", "1" }
        };

        var childValues = new SequenceDataNode("1");
        var parentValues = new SequenceDataNode("2");
        child.Add("f6", childValues);
        parent1.Add("f6", parentValues);

        var finalMapping = Serialization.PushComposition<CompositionTestClass, MappingDataNode>(new[] { parent1, parent2 }, child);
        var val = Serialization.Read<CompositionTestClass>(finalMapping, notNullableOverride: true);

        Assert.That(val.ChildValue, Is.EqualTo(1));
        Assert.That(val.Parent1Value, Is.EqualTo(1));
        Assert.That(val.Parent2Value, Is.EqualTo(1));
        Assert.That(val.NeverPushValueParent1, Is.EqualTo(0));
        Assert.That(val.NeverPushValueParent2, Is.EqualTo(0));
        Assert.That(val.AlwaysPushValues, Is.EqualTo(new[] { 1, 2 }));

        // Composition must not clone immutable source subtrees just to replace top-level fields.
        Assert.That(finalMapping["f1"], Is.SameAs(child["f1"]));
        var finalValues = finalMapping.Get<SequenceDataNode>("f6");
        Assert.That(finalValues[0], Is.SameAs(childValues[0]));
        Assert.That(finalValues[1], Is.SameAs(parentValues[0]));
    }

}
