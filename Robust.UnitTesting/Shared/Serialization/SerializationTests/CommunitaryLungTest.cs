using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.UnitTesting.Shared.Serialization.SerializationTests;

[TestFixture]
public sealed partial class CommunitaryLungTest : SerializationTest
{
    [Test]
    public void Test()
    {
        var dict = new Dictionary<int, string>();
        var def = new DataDefinition { Dict = dict };
        var copy = Serialization.CreateCopy(def, notNullableOverride: true);

        Assert.That(def.Dict == copy.Dict, Is.False);

        // Sanity check
#pragma warning disable CS1718 // Comparison made to same variable
        Assert.That(def.Dict == def.Dict, Is.True);
#pragma warning restore CS1718 // Comparison made to same variable

        Serialization.CopyTo(def, ref copy, notNullableOverride: true);

        Assert.That(def.Dict == copy.Dict, Is.False);

        // Sanity check
#pragma warning disable CS1718 // Comparison made to same variable
        Assert.That(def.Dict == def.Dict, Is.True);
#pragma warning restore CS1718 // Comparison made to same variable
    }

    [Test]
    public void NullableNonNullTest()
    {
        var dict = new Dictionary<int, string>();
        var def = new NullableDataDefinition { Dict = dict };
        var copy = Serialization.CreateCopy(def, notNullableOverride: true);

        Assert.That(def.Dict == copy.Dict, Is.False);
        Assert.That(copy.Dict, Is.Not.Null);

        // Sanity check
#pragma warning disable CS1718 // Comparison made to same variable
        Assert.That(def.Dict == def.Dict, Is.True);
#pragma warning restore CS1718 // Comparison made to same variable

        Serialization.CopyTo(def, ref copy, notNullableOverride: true);

        Assert.That(def.Dict == copy.Dict, Is.False);
        Assert.That(copy.Dict, Is.Not.Null);

        // Sanity check
#pragma warning disable CS1718 // Comparison made to same variable
        Assert.That(def.Dict == def.Dict, Is.True);
#pragma warning restore CS1718 // Comparison made to same variable
    }

    [Test]
    public void NullableNullTest()
    {
        var def = new NullableDataDefinition();
        var copy = Serialization.CreateCopy(def, notNullableOverride: true);

        Assert.That(def.Dict, Is.Null);
        Assert.That(copy.Dict, Is.Null);

        Serialization.CopyTo(def, ref copy, notNullableOverride: true);

        Assert.That(def.Dict, Is.Null);
        Assert.That(copy.Dict, Is.Null);
    }

    [DataDefinition]
    private sealed partial class DataDefinition
    {
        [DataField("dict")] public Dictionary<int, string> Dict;
    }

    [DataDefinition]
    private sealed partial class NullableDataDefinition
    {
        [DataField("dict")] public Dictionary<int, string>? Dict;
    }
}
