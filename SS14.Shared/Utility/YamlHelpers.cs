using SFML.System;
using SS14.Shared.Maths;
using System;
using YamlDotNet.RepresentationModel;

public static class YamlHelpers
{
    public static int AsInt(this YamlNode node)
    {
        return int.Parse(((YamlScalarNode)node).Value);
    }

    public static string AsString(this YamlNode node)
    {
        return ((YamlScalarNode)node).Value;
    }

    public static float AsFloat(this YamlNode node)
    {
        return float.Parse(((YamlScalarNode)node).Value);
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

        return new Vector2f(float.Parse(args[0]), float.Parse(args[1]));
    }

    public static Vector3f AsVector3f(this YamlNode node)
    {
        string raw = AsString(node);
        string[] args = raw.Split(',');
        if (args.Length != 3)
        {
            throw new ArgumentException(string.Format("Could not parse {0}: '{1}'", nameof(Vector3f), raw));
        }

        return new Vector3f(float.Parse(args[0]), float.Parse(args[1]), float.Parse(args[2]));
    }

    public static Vector4f AsVector4f(this YamlNode node)
    {
        string raw = AsString(node);
        string[] args = raw.Split(',');
        if (args.Length != 4)
        {
            throw new ArgumentException(string.Format("Could not parse {0}: '{1}'", nameof(Vector4f), raw));
        }

        return new Vector4f(float.Parse(args[0]), float.Parse(args[1]), float.Parse(args[2]), float.Parse(args[3]));
    }
}
