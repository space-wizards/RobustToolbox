using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;

namespace Robust.UnitTesting.Shared.Serialization;

/// <summary>
/// Tests that arrays and lists of virtual/abstract objects can be properly serialized and deserialized.
/// </summary>
public sealed partial class VirtualObjectArrayTest : SerializationTest
{
    [ImplicitDataDefinitionForInheritors]
    private abstract partial class BaseTestDataDef { }

    private sealed partial class SealedTestDataDef : BaseTestDataDef { }

    [Virtual]
    private partial class VirtualTestDataDef : BaseTestDataDef { }

    private sealed partial class ChildTestDef : VirtualTestDataDef { }

    [Test]
    public void SerializeVirtualObjectArrayTest()
    {
        var sequence = new SequenceDataNode
        {
            new MappingDataNode {Tag = $"!type:SealedTestDataDef"},
            new MappingDataNode {Tag = $"!type:VirtualTestDataDef"},
            new MappingDataNode {Tag = $"!type:ChildTestDef"}
        };

        {
            // Deserialize the above yaml
            var arr = Serialization.Read<BaseTestDataDef[]>(sequence, notNullableOverride: true);

            // Ensure that the !type: tags were properly parsed
            Assert.That(arr[0], Is.TypeOf(typeof(SealedTestDataDef)));
            Assert.That(arr[1], Is.TypeOf(typeof(VirtualTestDataDef)));
            Assert.That(arr[2], Is.TypeOf(typeof(ChildTestDef)));

            // Write the parsed object back to yaml
            var newSquence = Serialization.WriteValue(arr, notNullableOverride: true);

            // Check that the yaml doesn't differ in any way.
            var diff = newSquence.Except(sequence);
            Assert.That(diff, Is.Null);

            // And finally, double check that the serialized data can be re-deserialized (dataNode.Except isn't perfect).
            arr = Serialization.Read<BaseTestDataDef[]>(newSquence, notNullableOverride: true);
            Assert.That(arr[0], Is.TypeOf(typeof(SealedTestDataDef)));
            Assert.That(arr[1], Is.TypeOf(typeof(VirtualTestDataDef)));
            Assert.That(arr[2], Is.TypeOf(typeof(ChildTestDef)));
        }


        // Repeat the above, but using lists instead of arrays
        {
            var list = Serialization.Read<List<BaseTestDataDef>>(sequence, notNullableOverride: true);
            Assert.That(list[0], Is.TypeOf(typeof(SealedTestDataDef)));
            Assert.That(list[1], Is.TypeOf(typeof(VirtualTestDataDef)));
            Assert.That(list[2], Is.TypeOf(typeof(ChildTestDef)));

            var newSquence = Serialization.WriteValue(list, notNullableOverride: true);
            var diff = newSquence.Except(sequence);
            Assert.That(diff, Is.Null);

            list = Serialization.Read<List<BaseTestDataDef>>(sequence, notNullableOverride: true);
            Assert.That(list[0], Is.TypeOf(typeof(SealedTestDataDef)));
            Assert.That(list[1], Is.TypeOf(typeof(VirtualTestDataDef)));
            Assert.That(list[2], Is.TypeOf(typeof(ChildTestDef)));
        }

        // remove the first entry -- leave only entries that inherit from VirtualTestDataDef
        sequence.RemoveAt(0);

        // When writing, this will skip the !type tag for the first entry
        var expectedSequence = new SequenceDataNode
        {
            new MappingDataNode(),
            new MappingDataNode {Tag = $"!type:ChildTestDef"}
        };

        {
            var virtArr = Serialization.Read<VirtualTestDataDef[]>(sequence, notNullableOverride: true);
            Assert.That(virtArr[0], Is.TypeOf(typeof(VirtualTestDataDef)));
            Assert.That(virtArr[1], Is.TypeOf(typeof(ChildTestDef)));

            // The old sequence will now differ as it should not write the redundant !type tag
            var newSquence = Serialization.WriteValue(virtArr, notNullableOverride: true);
            var diff = newSquence.Except(sequence);
            Assert.That(diff, Is.Not.Null);

            diff = newSquence.Except(expectedSequence);
            Assert.That(diff, Is.Null);

            virtArr = Serialization.Read<VirtualTestDataDef[]>(newSquence, notNullableOverride: true);
            Assert.That(virtArr[0], Is.TypeOf(typeof(VirtualTestDataDef)));
            Assert.That(virtArr[1], Is.TypeOf(typeof(ChildTestDef)));
        }

        // And again, repeat for lists instead of arrays
        {
            var virtList = Serialization.Read<List<VirtualTestDataDef>>(sequence, notNullableOverride: true);
            Assert.That(virtList[0], Is.TypeOf(typeof(VirtualTestDataDef)));
            Assert.That(virtList[1], Is.TypeOf(typeof(ChildTestDef)));

            var newSquence = Serialization.WriteValue(virtList, notNullableOverride: true);
            var diff = newSquence.Except(sequence);
            Assert.That(diff, Is.Not.Null);

            diff = newSquence.Except(expectedSequence);
            Assert.That(diff, Is.Null);

            virtList = Serialization.Read<List<VirtualTestDataDef>>(newSquence, notNullableOverride: true);
            Assert.That(virtList[0], Is.TypeOf(typeof(VirtualTestDataDef)));
            Assert.That(virtList[1], Is.TypeOf(typeof(ChildTestDef)));
        }
    }
}
