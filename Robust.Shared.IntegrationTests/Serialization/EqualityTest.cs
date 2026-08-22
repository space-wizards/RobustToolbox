using NUnit.Framework;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.UnitTesting.Shared.Serialization;

[TestFixture]
internal sealed class EqualityTest : OurSerializationTest
{
    [Test]
    public void DataFieldEqualsUsesRuntimeDataDefinition()
    {
        EqualityBase left = new EqualityDerived { BaseValue = 1, DerivedValue = 2 };
        EqualityBase right = new EqualityDerived { BaseValue = 1, DerivedValue = 3 };

        Assert.That(Serialization.DataFieldEquals(left, right), Is.False);

        ((EqualityDerived) right).DerivedValue = 2;
        Assert.That(Serialization.DataFieldEquals(left, right), Is.True);
    }

    [Test]
    public void DataFieldEqualsUsesSerializedFieldsInsteadOfObjectEquals()
    {
        var left = new EqualityOverride { Value = 1 };
        var right = new EqualityOverride { Value = 2 };

        Assert.That(left.Equals(right), Is.True);
        Assert.That(Serialization.DataFieldEquals(left, right), Is.False);
        Assert.That(Serialization.DataFieldEquals<EqualityOverride[]>([left], [right]), Is.False);
    }
}

[DataDefinition]
public abstract partial class EqualityBase
{
    [DataField]
    public int BaseValue;
}

[DataDefinition]
public sealed partial class EqualityDerived : EqualityBase
{
    [DataField]
    public int DerivedValue;
}

[DataDefinition]
public sealed partial class EqualityOverride
{
    [DataField]
    public int Value;

    public override bool Equals(object? obj)
    {
        return obj is EqualityOverride;
    }

    public override int GetHashCode()
    {
        return 0;
    }
}
