using NUnit.Framework;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace Robust.UnitTesting.Shared.Serialization;

public sealed partial class DataStructTest : SerializationTest
{
    [DataDefinition]
    public partial struct DefaultIntDataStruct
    {
        public int A = 5;

        public DefaultIntDataStruct()
        {
        }
    }

    [Test]
    public void DefaultIntDataStructTest()
    {
        var mapping = new MappingDataNode();
        var val = Serialization.Read<DefaultIntDataStruct>(mapping);

        Assert.That(val.A, Is.EqualTo(5));
    }
}
