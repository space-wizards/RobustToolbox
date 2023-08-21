using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace Robust.UnitTesting.Shared.Serialization;

[TestFixture]
public sealed partial class CompositionTest : SerializationTest
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

        var finalMapping = Serialization.PushComposition<CompositionTestClass, MappingDataNode>(new[] { parent1, parent2 }, child);
        var val = Serialization.Read<CompositionTestClass>(finalMapping, notNullableOverride: true);

        Assert.That(val.ChildValue, Is.EqualTo(1));
        Assert.That(val.Parent1Value, Is.EqualTo(1));
        Assert.That(val.Parent2Value, Is.EqualTo(1));
        Assert.That(val.NeverPushValueParent1, Is.EqualTo(0));
        Assert.That(val.NeverPushValueParent2, Is.EqualTo(0));
    }
}
