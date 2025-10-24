using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Robust.Shared.Localization;

internal sealed record CldrIdentity(
    [property: JsonPropertyName("language")]
    string Language,
    [property: JsonPropertyName("territory")]
    string? Territory
)
{
    public static CldrIdentity FromCultureInfo(CultureInfo info)
    {
        var split = info.Name.Split("-", 2);
        if (split.Length == 1)
        {
            return new(split[0], null);
        }
        else if (split.Length == 2)
        {
            return new(split[0], split[1]);
        }

        throw new InvalidOperationException($"Could not determine CLDR identity for {info}");
    }
    public override string ToString() =>
        Territory is null ? Language : $"{Language}-{Territory}";
}

[JsonConverter(typeof(CldrCultureKeyJsonConverter))]
internal record struct CldrCultureKey(CultureInfo Culture);

internal class CldrCultureKeyJsonConverter : JsonConverter<CldrCultureKey>
{
    public override CldrCultureKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new(CultureInfo.GetCultureInfo(reader.GetString()!));
    }
    public override CldrCultureKey ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Read(ref reader, typeToConvert, options);
    }
    public override void Write(Utf8JsonWriter writer, CldrCultureKey key, JsonSerializerOptions options)
    {
        writer.WriteStringValue(key.Culture.Name);
    }
    public override void WriteAsPropertyName(Utf8JsonWriter writer, CldrCultureKey key, JsonSerializerOptions options)
    {
        Write(writer, key, options);
    }
}

[JsonConverter(typeof(CldrListPatternKeyJsonConverter))]
internal record struct CldrListPatternKey(ListType Type, ListWidth Width);

internal class CldrListPatternKeyJsonConverter : JsonConverter<CldrListPatternKey>
{
    public override CldrListPatternKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString()! switch {
            "listPattern-type-or" => new(ListType.Or, ListWidth.Wide),
            "listPattern-type-or-narrow" => new(ListType.Or, ListWidth.Narrow),
            "listPattern-type-or-short" => new(ListType.Or, ListWidth.Short),
            "listPattern-type-standard" => new(ListType.And, ListWidth.Wide),
            "listPattern-type-standard-narrow" => new(ListType.And, ListWidth.Narrow),
            "listPattern-type-standard-short" => new(ListType.And, ListWidth.Short),
            "listPattern-type-unit" => new(ListType.Unit, ListWidth.Wide),
            "listPattern-type-unit-narrow" => new(ListType.Unit, ListWidth.Narrow),
            "listPattern-type-unit-short" => new(ListType.Unit, ListWidth.Short),
        };
    }
    public override CldrListPatternKey ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Read(ref reader, typeToConvert, options);
    }
    public override void Write(Utf8JsonWriter writer, CldrListPatternKey key, JsonSerializerOptions options)
    {
        writer.WriteStringValue(key.Type switch {
            ListType.Or => "listPattern-type-or",
            ListType.And => "listPattern-type-standard",
            ListType.Unit => "listPattern-type-unit",
        } + key.Width switch {
            ListWidth.Wide => "",
            ListWidth.Narrow => "-narrow",
            ListWidth.Short => "-short",
        });
    }
    public override void WriteAsPropertyName(Utf8JsonWriter writer, CldrListPatternKey key, JsonSerializerOptions options)
    {
        Write(writer, key, options);
    }
}

internal sealed record CldrListPatternParts(
    [property: JsonPropertyName("start")]
    string Start,
    [property: JsonPropertyName("middle")]
    string Middle,
    [property: JsonPropertyName("end")]
    string End,
    [property: JsonPropertyName("2")]
    string? Two,
    [property: JsonPropertyName("3")]
    string? Three
)
{
    public string FormatList(List<string> strings)
    {
        if (strings.Count == 0)
            return string.Empty;

        if (strings.Count == 1)
            return strings[0];

        if (strings.Count == 2 && Two is { } two)
            return string.Format(two, strings[0], strings[1]);

        if (strings.Count == 3 && Three is { } three)
            return string.Format(three, strings[0], strings[1], strings[2]);

        var ret = string.Format(End, strings[^2], strings[^1]);
        for (var i = 3; i < strings.Count; i++)
        {
            ret = string.Format(Middle, strings[^i], ret);
        }
        ret = string.Format(Start, strings[0], ret);

        return ret;
    }
}

internal sealed record CldrResource(
    [property: JsonPropertyName("identity"), JsonRequired]
    CldrIdentity Identity,
    [property: JsonPropertyName("listPatterns")]
    Dictionary<CldrListPatternKey, CldrListPatternParts>? ListPatterns
)
{
    public static CldrResource Merge(CldrResource parent, CldrResource child)
    {
        if (parent.Identity != child.Identity)
            throw new InvalidOperationException($"Attempting to merge two CLDR resources with differing identities '{parent.Identity}' and '{child.Identity}'");

        return child with { ListPatterns = child.ListPatterns ?? parent.ListPatterns };
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum CldrPersonListGender
{
    [JsonStringEnumMemberName("neutral")]
    Neutral,
    [JsonStringEnumMemberName("mixedNeutral")]
    MixedNeutral,
    [JsonStringEnumMemberName("maleTaints")]
    MaleTaints
}

internal sealed record CldrSupplementalGenderData(
    [property: JsonPropertyName("personList"), JsonRequired]
    Dictionary<CldrCultureKey, CldrPersonListGender> PersonList
)
{
    public static CldrSupplementalGenderData Merge(CldrSupplementalGenderData parent, CldrSupplementalGenderData child)
    {
        var personList = new Dictionary<CldrCultureKey, CldrPersonListGender>(child.PersonList);
        foreach (var kvp in parent.PersonList)
        {
            if (!personList.ContainsKey(kvp.Key))
                personList.Add(kvp.Key, kvp.Value);
        }
        return new(personList);
    }
}

internal sealed record CldrSupplementalData(
    [property: JsonPropertyName("gender")]
    CldrSupplementalGenderData? Gender
)
{
    public static CldrSupplementalData Merge(CldrSupplementalData parent, CldrSupplementalData child)
    {
        if (parent.Gender is not null && child.Gender is not null)
        {
            return child with { Gender = CldrSupplementalGenderData.Merge(parent.Gender, child.Gender) };
        }

        return child with { Gender = child.Gender ?? parent.Gender };
    }
}

internal sealed record CldrBundle(
    [property: JsonPropertyName("main")]
    Dictionary<string, CldrResource> Resources,
    [property: JsonPropertyName("supplemental")]
    CldrSupplementalData? Supplemental
);
