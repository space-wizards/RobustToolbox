using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Exceptions;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.UnitTesting.Shared.Serialization.SerializationTests;

public sealed partial class ReadValueProviderTests : SerializationTest
{
    //test for: datadefinition (value, mapping), selfserialize

    #region TypeDefinitions

    private sealed class SelfSerializeValueProviderTestDummy : ISelfSerialize, IBaseInterface
    {
        public string Data = string.Empty;

        public void Deserialize(string value)
        {
            Data = value;
        }

        public string Serialize()
        {
            return Data;
        }
    }

    [DataDefinition]
    public sealed partial class DataDefinitionValueProviderTestDummy : IBaseInterface
    {
        [DataField("data")] public string Data = string.Empty;
    }

    public sealed class OtherDataDefinitionValueProviderTestDummy : IBaseInterface{}

    private interface IBaseInterface {}

    #endregion

    [Test]
    public void SelfSerializeTest()
    {
        var data = "someData";
        var instance = new SelfSerializeValueProviderTestDummy();
        var result = Serialization.Read(new ValueDataNode(data), instanceProvider: () => instance, notNullableOverride: true);
        Assert.That(result, Is.SameAs(instance));
        Assert.That(result.Data, Is.EqualTo(instance.Data));
    }

    [Test]
    public void SelfSerializeBaseTest()
    {
        var data = "someData";
        var instance = new SelfSerializeValueProviderTestDummy();
        var result = Serialization.Read<IBaseInterface>(new ValueDataNode(data){Tag = $"!type:{nameof(SelfSerializeValueProviderTestDummy)}"}, instanceProvider: () => instance, notNullableOverride: true);
        Assert.That(result, Is.SameAs(instance));
        Assert.That(((SelfSerializeValueProviderTestDummy)result).Data, Is.EqualTo(instance.Data));
    }

    [Test]
    public void DataDefinitionMappingTest()
    {
        var data = "someData";
        var mapping = new MappingDataNode { { "data", data } };
        var instance = new DataDefinitionValueProviderTestDummy();
        var result = Serialization.Read(mapping, instanceProvider: () => instance, notNullableOverride: true);
        Assert.That(result, Is.SameAs(instance));
        Assert.That(result.Data, Is.EqualTo(instance.Data));
    }

    [Test]
    public void DataDefinitionMappingBaseTest()
    {
        var data = "someData";
        var mapping = new MappingDataNode(new Dictionary<string, DataNode>{{ "data", new ValueDataNode(data) }})
        {
            Tag = $"!type:{nameof(DataDefinitionValueProviderTestDummy)}"
        };
        var instance = new DataDefinitionValueProviderTestDummy();
        var result = Serialization.Read<IBaseInterface>(mapping, instanceProvider: () => instance, notNullableOverride: true);
        Assert.That(result, Is.SameAs(instance));
        Assert.That(((DataDefinitionValueProviderTestDummy)result).Data, Is.EqualTo(instance.Data));
    }

    [Test]
    public void DataDefinitionValueBaseTest()
    {
        var instance = new DataDefinitionValueProviderTestDummy();
        var result = Serialization.Read<IBaseInterface>(new ValueDataNode{Tag = $"!type:{nameof(DataDefinitionValueProviderTestDummy)}"}, instanceProvider: () => instance, notNullableOverride: true);
        Assert.That(result, Is.SameAs(instance));
    }

    [Test]
    public void DataDefinitionValueBaseInvalidTest()
    {
        var instance = new OtherDataDefinitionValueProviderTestDummy();
        Assert.That(() => Serialization.Read<IBaseInterface>(new ValueDataNode{Tag = $"!type:{nameof(DataDefinitionValueProviderTestDummy)}"}, instanceProvider: () => instance, notNullableOverride: true),Throws.InstanceOf<InvalidInstanceReturnedException>());
    }
}
