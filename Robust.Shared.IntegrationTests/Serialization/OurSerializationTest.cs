using System.Reflection;

namespace Robust.UnitTesting.Shared.Serialization;

public abstract class OurSerializationTest : SerializationTest
{
    protected override Assembly[] GetContentAssemblies()
    {
        return [typeof(OurSerializationTest).Assembly, ..base.GetContentAssemblies()];
    }
}
