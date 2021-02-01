using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

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
        public struct PrototypeLayerData : IExposeData
        {
            public string? Shader;
            public string? TexturePath;
            public string? RsiPath;
            public string? State;
            public Vector2 Scale;
            public Angle Rotation;
            public bool Visible;
            public Color Color;
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

            public void ExposeData(ObjectSerializer serializer)
            {
                serializer.DataField(ref Shader, "shader", null);
                serializer.DataField(ref TexturePath, "texture", null);
                serializer.DataField(ref RsiPath, "sprite", null);
                serializer.DataField(ref State, "state", null);
                serializer.DataField(ref Scale, "scale", Vector2.One);
                serializer.DataField(ref Rotation, "rotation", Angle.Zero);
                serializer.DataField(ref Visible, "visible", true);
                serializer.DataField(ref Color, "color", Color.White);
                serializer.DataField(ref MapKeys, "map", null);
            }

            public IDeepClone DeepClone()
            {
                return new PrototypeLayerData()
                {
                    Shader = Shader,
                    TexturePath = TexturePath,
                    RsiPath = RsiPath,
                    State = State,
                    Scale = IDeepClone.CloneValue(Scale),
                    Rotation = IDeepClone.CloneValue(Rotation),
                    Visible = Visible,
                    Color = IDeepClone.CloneValue(Color),
                    MapKeys = IDeepClone.CloneValue(MapKeys)
                };
            }
        }
    }
}
