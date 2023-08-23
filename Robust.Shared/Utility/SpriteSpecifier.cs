using System;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Utility
{
    // >tfw you're not using Rust and you don't have easy sum types.
    // pub enum SpriteSpecifier {
    //      Rsi { path: ResPath, state: String, },
    //      Texture(ResPath),
    // }
    /// <summary>
    ///     Is a reference to EITHER an RSI + RSI State, OR a bare texture path.
    /// </summary>
    [Serializable, NetSerializable]
    public abstract partial class SpriteSpecifier
    {
        public static readonly SpriteSpecifier Invalid = new Texture(ResPath.Self);

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

        [Serializable, NetSerializable]
        [DataDefinition] // uses custom serializer, but required for [IncludeDataField]
        public sealed partial class Rsi : SpriteSpecifier
        {
            [DataField("sprite")]
            public ResPath RsiPath { get; internal set; }

            [DataField("state")]
            public string RsiState { get; internal set; }

            public Rsi(ResPath rsiPath, string rsiState)
            {
                RsiPath = rsiPath;
                RsiState = rsiState;
            }

            public override bool Equals(object? obj)
            {
                return (obj is Rsi rsi) && rsi.RsiPath == RsiPath && rsi.RsiState == RsiState;
            }

            public override int GetHashCode()
            {
                return RsiPath.GetHashCode() ^ RsiState.GetHashCode();
            }
        }

        [Serializable, NetSerializable]
        public sealed class Texture : SpriteSpecifier
        {
            public ResPath TexturePath { get; internal set; }

            // For serialization
            private Texture()
            {
                TexturePath = default!;
            }

            public Texture(ResPath texturePath)
            {
                TexturePath = texturePath;
            }

            public override bool Equals(object? obj)
            {
                return (obj is Texture texture) && texture.TexturePath == TexturePath;
            }

            public override int GetHashCode()
            {
                return TexturePath.GetHashCode();
            }
        }

        [Serializable, NetSerializable]
        public sealed class EntityPrototype : SpriteSpecifier
        {
            public readonly string EntityPrototypeId;

            public EntityPrototype(string entityPrototypeId)
            {
                EntityPrototypeId = entityPrototypeId;
            }

            public override bool Equals(object? obj)
            {
                if (obj is EntityPrototype prototypeIcon)
                    return EntityPrototypeId == prototypeIcon.EntityPrototypeId;
                return false;
            }

            public override int GetHashCode()
            {
                return EntityPrototypeId.GetHashCode();
            }
        }
    }
}
