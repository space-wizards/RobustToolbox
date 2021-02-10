using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using System;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization
{
    /// <summary>
    /// Helper for doing custom value representation in YAML.
    /// </summary>
    /// <typeparam name="T">The type for which this gives a custom representation.</typeparam>
    public class YamlCustomFormatSerializer<T> : YamlObjectSerializer.TypeSerializer
    {
        private readonly WithFormat<T> _formatter;

        public YamlCustomFormatSerializer(WithFormat<T> formatter)
        {
            _formatter = formatter;
        }

        public override object NodeToType(Type type, YamlNode node, YamlObjectSerializer serializer)
        {
            return _formatter.FromCustomFormat(serializer.NodeToType(_formatter.Format, node));
        }

        public override YamlNode TypeToNode(object obj, YamlObjectSerializer serializer)
        {
            var t = (T)obj;
            return serializer.TypeToNode(_formatter.ToCustomFormat(t));
        }
    }

    /// <summary>
    /// Convenience class for static access to custom formatters.
    /// </summary>
    public static class WithFormat
    {
        public static WithFormat<int> Flags<T>()
        {
            return IoCManager.Resolve<ICustomFormatManager>().FlagFormat<T>();
        }

        public static WithFormat<int> Constants<T>()
        {
            return IoCManager.Resolve<ICustomFormatManager>().ConstantFormat<T>();
        }
    }
}
