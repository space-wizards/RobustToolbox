using System;
using SS14.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace SS14.Shared.Utility
{
    // >tfw you're not using Rust and you don't have easy sum types.
    // pub enum SpriteSpecifier {
    //      Rsi { path: ResourcePath, state: String, },
    //      Texture(ResourcePath),
    // }
    /// <summary>
    ///     Is a reference to EITHER an RSI + RSI State, OR a bare texture path.
    /// </summary>
    [Serializable, NetSerializable]
    public class SpriteSpecifier
    {
        public static readonly SpriteSpecifier Invalid = new Texture(new ResourcePath("."));

        public static SpriteSpecifier FromYaml(YamlNode node)
        {
            if (node is YamlScalarNode)
            {
                return new Texture(node.AsResourcePath());
            }
            if (node is YamlMappingNode mapping)
            {
                return new Rsi(mapping["sprite"].AsResourcePath(), mapping["state"].AsString());
            }
            throw new InvalidOperationException();
        }

        public sealed class Rsi : SpriteSpecifier
        {
            public readonly ResourcePath RsiPath;
            public readonly string RsiState;

            public Rsi(ResourcePath rsiPath, string rsiState)
            {
                RsiPath = rsiPath;
                RsiState = rsiState;
            }
        }

        public sealed class Texture : SpriteSpecifier
        {
            public readonly ResourcePath TexturePath;

            public Texture(ResourcePath texturePath)
            {
                TexturePath = texturePath;
            }
        }
    }
}
