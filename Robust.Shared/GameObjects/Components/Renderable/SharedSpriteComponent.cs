using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using YamlDotNet.Serialization;

namespace Robust.Shared.GameObjects.Components.Renderable
{
    public class SharedSpriteComponent : Component
    {
        public override string Name => "Sprite";
        public override uint? NetID => NetIDs.SPRITE;

        /// <summary>
        ///     The resource path from which all texture paths are relative to.
        /// </summary>
        public static readonly ResourcePath TextureRoot = new("/Textures");

        [Serializable, NetSerializable]
        protected class SpriteComponentState : ComponentState
        {
            public readonly bool Visible;
            public readonly int DrawDepth;
            public readonly Vector2 Scale;
            public readonly Angle Rotation;
            public readonly Vector2 Offset;
            public readonly Color Color;
            public readonly bool Directional;
            public readonly string? BaseRsiPath;
            public readonly List<PrototypeLayerData> Layers;
            public readonly uint RenderOrder;

            public SpriteComponentState(
                bool visible,
                int drawDepth,
                Vector2 scale,
                Angle rotation,
                Vector2 offset,
                Color color,
                bool directional,
                string? baseRsiPath,
                List<PrototypeLayerData> layers,
                uint renderOrder)
                : base(NetIDs.SPRITE)
            {
                Visible = visible;
                DrawDepth = drawDepth;
                Scale = scale;
                Rotation = rotation;
                Offset = offset;
                Color = color;
                Directional = directional;
                BaseRsiPath = baseRsiPath;
                Layers = layers;
                RenderOrder = renderOrder;
            }
        }

        [Serializable, NetSerializable]
        [DataDefinition]
        public class PrototypeLayerData
        {
            [DataField("shader")]
            public string? Shader;
            [DataField("texture")]
            public string? TexturePath;
            [DataField("sprite")]
            public string? RsiPath;
            [DataField("state")]
            public string? State;
            [DataField("scale")]
            public Vector2 Scale = Vector2.One;
            [DataField("rotation")]
            public Angle Rotation = Angle.Zero;
            [DataField("visible")]
            public bool Visible = true;
            [DataField("color")]
            public Color Color = Color.White;
            [DataField("map")]
            public List<string>? MapKeys;

            public static PrototypeLayerData New()
            {
                return new()
                {
                    Scale = Vector2.One,
                    Color = Color.White,
                    Visible = true,
                };
            }
        }
    }
}
