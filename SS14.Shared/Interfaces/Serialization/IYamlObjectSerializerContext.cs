using System;
using YamlDotNet.RepresentationModel;

namespace SS14.Shared.Serialization
{
    /// <summary>
    ///     Basically, when you're serializing say a map file, you gotta be a liiiittle smarter than "dump all these variables to YAML".
    ///     Stuff like entity references need to handled, for example.
    ///     This can do that.
    /// </summary>
    public interface IYamlObjectSerializerContext
    {
        bool TryTypeToNode(object obj, out YamlNode node);
        bool TryNodeToType(YamlNode node, Type type, out object obj);
        bool IsValueDefault<T>(string field, T value);
    }
}
