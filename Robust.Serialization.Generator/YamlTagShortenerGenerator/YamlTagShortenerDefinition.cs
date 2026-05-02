namespace Robust.Serialization.Generator.YamlTagShortenerGenerator;

internal sealed record YamlTagShortenerDefinition(string BaseTypeName, string BaseTypeNamespace, List<(string, string)> CustomChildTags);
