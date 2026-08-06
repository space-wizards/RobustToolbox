using NUnit.Framework;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace Robust.UnitTesting.Shared.Serialization;

internal sealed partial class DataStructTest : OurSerializationTest
{
    [DataDefinition]
    public partial struct DefaultIntDataStruct
    {
        [DataField]
        public int A = 5;

        [DataField]
        public int B;

        public DefaultIntDataStruct()
        {
            B = 1;
        }
    }

    [DataDefinition]
    public partial struct DefaultIntDataStructNoConstructor
    {
        [DataField]
        public int A = 5;

        [DataField]
        public int B;
    }

    [Test]
    public void DefaultIntDataStructTest()
    {
        var mapping = new MappingDataNode();
        var val = Serialization.Read<DefaultIntDataStruct>(mapping);
        var val2 = Serialization.Read<DefaultIntDataStructNoConstructor>(mapping);

        Assert.That(val.A, Is.EqualTo(5));
        Assert.That(val.B, Is.EqualTo(1));
        Assert.That(val2.A, Is.EqualTo(5));
        Assert.That(val2.B, Is.EqualTo(0));

        mapping = new MappingDataNode {{"a", "10"}};
        val = Serialization.Read<DefaultIntDataStruct>(mapping);
        val2 = Serialization.Read<DefaultIntDataStructNoConstructor>(mapping);

        Assert.That(val.A, Is.EqualTo(10));
        Assert.That(val.B, Is.EqualTo(1));
        Assert.That(val2.A, Is.EqualTo(10));
        Assert.That(val2.B, Is.EqualTo(0));

        mapping = new MappingDataNode {{"b", "10"}};
        val = Serialization.Read<DefaultIntDataStruct>(mapping);
        val2 = Serialization.Read<DefaultIntDataStructNoConstructor>(mapping);

        Assert.That(val.A, Is.EqualTo(5));
        Assert.That(val.B, Is.EqualTo(10));
        Assert.That(val2.A, Is.EqualTo(5));
        Assert.That(val2.B, Is.EqualTo(10));
    }
}
