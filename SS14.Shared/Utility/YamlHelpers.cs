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
        // Easy conversions for YamlScalarNodes.
        // All of these take regular nodes, to make the API easier and less copy paste.
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

        public static Vector2 AsVector2(this YamlNode node)
        {
            string raw = AsString(node);
            string[] args = raw.Split(',');
            if (args.Length != 2)
            {
                throw new ArgumentException(string.Format("Could not parse {0}: '{1}'", nameof(Vector2), raw));
            }

            return new Vector2(float.Parse(args[0], CultureInfo.InvariantCulture),
                                float.Parse(args[1], CultureInfo.InvariantCulture));
        }

        public static Vector2i AsVector2i(this YamlNode node)
        {
            string raw = AsString(node);
            string[] args = raw.Split(',');
            if (args.Length != 2)
            {
                throw new ArgumentException(string.Format("Could not parse {0}: '{1}'", nameof(Vector2), raw));
            }

            return new Vector2i(int.Parse(args[0], CultureInfo.InvariantCulture),
                                int.Parse(args[1], CultureInfo.InvariantCulture));
        }

        public static Vector3 AsVector3(this YamlNode node)
        {
            string raw = AsString(node);
            string[] args = raw.Split(',');
            if (args.Length != 3)
            {
                throw new ArgumentException(string.Format("Could not parse {0}: '{1}'", nameof(Vector3), raw));
            }

            return new Vector3(float.Parse(args[0], CultureInfo.InvariantCulture),
                                float.Parse(args[1], CultureInfo.InvariantCulture),
                                float.Parse(args[2], CultureInfo.InvariantCulture));
        }

        public static Vector4 AsVector4(this YamlNode node)
        {
            string raw = AsString(node);
            string[] args = raw.Split(',');
            if (args.Length != 4)
            {
                throw new ArgumentException(string.Format("Could not parse {0}: '{1}'", nameof(Vector4), raw));
            }

            return new Vector4(float.Parse(args[0], CultureInfo.InvariantCulture),
                                float.Parse(args[1], CultureInfo.InvariantCulture),
                                float.Parse(args[2], CultureInfo.InvariantCulture),
                                float.Parse(args[3], CultureInfo.InvariantCulture));
        }

        public static T AsEnum<T>(this YamlNode node)
        {
            return (T)Enum.Parse(typeof(T), node.AsString(), true);
        }

        public static Color AsHexColor(this YamlNode node, Color? fallback = null)
        {
            return Color.FromHex(node.AsString(), fallback);
        }

        // Mapping specific helpers.

        /// <summary>
        /// Get the node corresponding to a scalar node with value <paramref name="key" /> inside <paramref name="mapping" />,
        /// attempting to cast it to <typeparamref name="T" />.
        /// </summary>
        /// <param name="mapping">The mapping to retrieve the node from.</param>
        /// <param name="name">The value of the scalar node that will be looked up.</param>
        /// <returns>The corresponding node casted to <typeparamref name="T" />.</returns>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if <paramref name="mapping" /> does not contain a scalar with value <paramref name="key" />.
        /// </exception>
        /// <exception cref="InvalidCastException">
        /// Thrown if the node could be found, but could not be cast to <typeparamref name="T" />.
        /// </exception>
        /// <seealso cref="GetNode" />
        /// <seealso cref="TryGetNode{T}" />
        public static T GetNode<T>(this YamlMappingNode mapping, string key) where T : YamlNode
        {
            return (T)mapping[new YamlScalarNode(key)];
        }

        /// <summary>
        /// Same as <see cref="GetNode{T}" />, but has <c>T</c> at <c>YamlNode</c>.
        /// </summary>
        /// <param name="mapping">The mapping to retrieve the node from.</param>
        /// <param name="key">The value of the scalar node that will be looked up.</param>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if <paramref name="mapping" /> does not contain a scalar with value <paramref name="key" />.
        /// </exception>
        /// <returns>The node found.</returns>
        public static YamlNode GetNode(this YamlMappingNode mapping, string key)
        {
            return mapping.GetNode<YamlNode>(key);
        }

        /// <summary>
        /// Attempts to fetch a node like <see cref="GetNode{T}" />,
        /// but does not throw a <c>KeyNotFoundException</c> if the node doesn't exist.
        /// Instead it returns whether the node was successfully found.
        /// </summary>
        /// <param name="mapping">The mapping to retrieve the node from.</param>
        /// <param name="key">The value of the scalar node that will be looked up.</param>
        /// <param name="returnNode">The node casted to <typeparamref name="T" />, <c>null</c> if the node could not be found.</param>
        /// <returns>True if the value could be found, false otherwise.</returns>
        /// <exception cref="InvalidCastException" />
        /// Thrown if the node could be found, but was the wrong type.
        /// This is intentional, as this most of the time means user error in the prototype definition.
        /// </exception>
        public static bool TryGetNode<T>(this YamlMappingNode mapping, string key, out T returnNode) where T : YamlNode
        {
            var dummy = new YamlScalarNode(key);
            if (mapping.Children.TryGetValue(dummy, out var node))
            {
                returnNode = (T)node;
                return true;
            }

            returnNode = null;
            return false;
        }

        /// <summary>
        /// Attempts to fetch a node like <see cref="GetNode" />,
        /// but does not throw a <c>KeyNotFoundException</c> if the node doesn't exist.
        /// Instead it returns whether the node was successfully found.
        /// </summary>
        /// <param name="mapping">The mapping to retrieve the node from.</param>
        /// <param name="key">The value of the scalar node that will be looked up.</param>
        /// <param name="returnNode">The node found, <c>null</c> if it could not be found.</param>
        /// <returns>True if the value could be found, false otherwise.</returns>
        public static bool TryGetNode(this YamlMappingNode mapping, string key, out YamlNode returnNode)
        {
            return mapping.Children.TryGetValue(new YamlScalarNode(key), out returnNode);
        }

        /// <summary>
        /// Copies a <see cref="YamlMappingNode" /> to a dictionary by using scalar values as keys for the dictionary.
        /// </summary>
        /// <param name="mapping">The mapping to copy from.</param>
        /// <returns>The dictionary.</returns>
        public static Dictionary<string, YamlNode> YamlMappingToDict(YamlMappingNode mapping)
        {
            return mapping.ToDictionary(p => p.Key.AsString(), p => p.Value);
        }
    }
}
