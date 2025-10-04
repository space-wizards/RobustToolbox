using System;
using NUnit.Framework;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers;

[TestFixture]
[TestOf(typeof(TimespanSerializer))]
internal sealed class TimespanSerializerTest : SerializationTest
{
    [Test]
    public void ReadTest()
    {
        var input1 = "21600";
        var input2 = "21600s";
        var input3 = "360m";
        var input4 = "6h";

        var result1 = Serialization.Read<TimeSpan>(Serialization.WriteValueAs<ValueDataNode>(input1));
        var result2 = Serialization.Read<TimeSpan>(Serialization.WriteValueAs<ValueDataNode>(input2));
        var result3 = Serialization.Read<TimeSpan>(Serialization.WriteValueAs<ValueDataNode>(input3));
        var result4 = Serialization.Read<TimeSpan>(Serialization.WriteValueAs<ValueDataNode>(input4));

        var time = TimeSpan.FromHours(6);

        Assert.That(result1, Is.EqualTo(time));
        Assert.That(result2, Is.EqualTo(time));
        Assert.That(result3, Is.EqualTo(time));
        Assert.That(result4, Is.EqualTo(time));
    }

    [Test]
    public void DecimalTest()
    {
        var input1 = "6.0h";
        var input2 = "6.0001h";

        var result1 = Serialization.Read<TimeSpan>(Serialization.WriteValueAs<ValueDataNode>(input1));
        var result2 = Serialization.Read<TimeSpan>(Serialization.WriteValueAs<ValueDataNode>(input2));

        var time = TimeSpan.FromHours(6);

        Assert.That(result1, Is.EqualTo(time));
        Assert.That(result2, Is.Not.EqualTo(time));
    }
}
