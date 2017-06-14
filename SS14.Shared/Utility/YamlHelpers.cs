using SFML.System;
using SFML.Graphics;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace SS14.Shared.Utility
{
    public static class YamlHelpers
    {
        public static int AsInt(this YamlNode node)
        {
            return int.Parse(((YamlScalarNode)node).Value, CultureInfo.InvariantCulture);
        }

        public static string AsString(this YamlNode node)
        {
            return ((YamlScalarNode)node).Value;
        }

        public static float AsFloat(this YamlNode node)
        {
            return float.Parse(((YamlScalarNode)node).Value, CultureInfo.InvariantCulture);
        }

        public static bool AsBool(this YamlNode node)
        {
            return bool.Parse(((YamlScalarNode)node).Value);
        }

        public static Vector2f AsVector2f(this YamlNode node)
        {
            string raw = AsString(node);
            string[] args = raw.Split(',');
            if (args.Length != 2)
            {
                throw new ArgumentException(string.Format("Could not parse {0}: '{1}'", nameof(Vector2f), raw));
            }

            return new Vector2f(float.Parse(args[0], CultureInfo.InvariantCulture),
                                float.Parse(args[1], CultureInfo.InvariantCulture));
        }

        public static Vector2i AsVector2i(this YamlNode node)
        {
            string raw = AsString(node);
            string[] args = raw.Split(',');
            if (args.Length != 2)
            {
                throw new ArgumentException(string.Format("Could not parse {0}: '{1}'", nameof(Vector2f), raw));
            }

            return new Vector2i(int.Parse(args[0], CultureInfo.InvariantCulture),
                                int.Parse(args[1], CultureInfo.InvariantCulture));
        }

        public static Vector3f AsVector3f(this YamlNode node)
        {
            string raw = AsString(node);
            string[] args = raw.Split(',');
            if (args.Length != 3)
            {
                throw new ArgumentException(string.Format("Could not parse {0}: '{1}'", nameof(Vector3f), raw));
            }

            return new Vector3f(float.Parse(args[0], CultureInfo.InvariantCulture),
                                float.Parse(args[1], CultureInfo.InvariantCulture),
                                float.Parse(args[2], CultureInfo.InvariantCulture));
        }

        public static Vector4f AsVector4f(this YamlNode node)
        {
            string raw = AsString(node);
            string[] args = raw.Split(',');
            if (args.Length != 4)
            {
                throw new ArgumentException(string.Format("Could not parse {0}: '{1}'", nameof(Vector4f), raw));
            }

            return new Vector4f(float.Parse(args[0], CultureInfo.InvariantCulture),
                                float.Parse(args[1], CultureInfo.InvariantCulture),
                                float.Parse(args[2], CultureInfo.InvariantCulture),
                                float.Parse(args[3], CultureInfo.InvariantCulture));
        }

        public static T AsEnum<T>(this YamlNode node)
        {
            return (T) Enum.Parse(typeof (T), node.AsString(), true);
        }

        public static Color AsHexColor(this YamlNode node, Color? fallback = null)
        {
            return ColorUtils.FromHex(node.AsString(), fallback);
        }

        public static Dictionary<string, YamlNode> YamlMappingToDict(YamlMappingNode mapping)
        {
            return mapping.ToDictionary(p => p.Key.AsString(), p => p.Value);
        }
    }
}
