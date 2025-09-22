using System;
using System.Globalization;
using NUnit.Framework;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Robust.UnitTesting.Shared.Serialization.TypeSerializers;

[TestFixture]
[TestOf(typeof(DateTimeSerializer))]
internal sealed class DateTimeSerializerTest : SerializationTest
{
    [Test]
    public void WriteTest()
    {
        var dateTime = DateTime.UtcNow;
        var result = Serialization.WriteValueAs<ValueDataNode>(dateTime);

        var parsed = DateTime.Parse(result.Value, null, DateTimeStyles.RoundtripKind);
        Assert.That(parsed, Is.EqualTo(dateTime));
    }

    [Test]
    public void ReadTest()
    {
        var result = Serialization.Read<DateTime>(new ValueDataNode("2020-07-10 15:00:00.000"));

        Assert.That(result, Is.EqualTo(new DateTime(2020, 07, 10, 15, 0, 0)));
    }
}
