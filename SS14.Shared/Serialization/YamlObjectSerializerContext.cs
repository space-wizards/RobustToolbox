using System;
using YamlDotNet.RepresentationModel;

namespace SS14.Shared.Serialization
{
    /// <summary>
    ///     Basically, when you're serializing say a map file, you gotta be a liiiittle smarter than "dump all these variables to YAML".
    ///     Stuff like entity references need to handled, for example.
    ///     This can do that.
    /// </summary>
    public abstract class YamlObjectSerializerContext
    {
        public int StackDepth { get; protected internal set; } = 0;

        public virtual bool TryTypeToNode(object obj, out YamlNode node)
        {
            node = null;
            return false;
        }

        public virtual bool TryNodeToType(YamlNode node, Type type, out object obj)
        {
            obj = default(object);
            return false;
        }

        public virtual bool IsValueDefault<T>(string field, T value)
        {
            return false;
        }

        public virtual bool TryGetCachedField<T>(string field, out T value)
        {
            value = default(T);
            return false;
        }

        public virtual void SetCachedField<T>(string field, T value)
        {
        }

        public virtual bool TryGetDataCache(string field, out object value)
        {
            value = null;
            return false;
        }

        public virtual void SetDataCache(string field, object value)
        {
        }
    }
}
