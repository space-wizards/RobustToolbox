using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using DrawDepthTag = Robust.Shared.GameObjects.DrawDepth;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Server.GameObjects
{
    public class SpriteComponent : SharedSpriteComponent, ISpriteRenderableComponent, ISerializationHooks
    {
        const string LayerSerializationCache = "spritelayersrv";

        [ViewVariables]
        [DataField("layers", priority: 2, readOnly: true)]
        private List<PrototypeLayerData> Layers = new();

        [DataField("visible")]
        private bool _visible = true;

        [DataFieldWithConstant("drawdepth", typeof(DrawDepthTag))]
        private int _drawDepth = DrawDepthTag.Default;

        [DataField("scale")]
        private Vector2 _scale = Vector2.One;

        [DataField("offset")]
        private Vector2 _offset = Vector2.Zero;

        [DataField("color")]
        private Color _color = Color.White;

        [DataField("directional")]
        private bool _directional = true;

        [DataField("sprite")]
        private string? _baseRSIPath;

        [DataField("rotation")]
        private Angle _rotation = Angle.Zero;

        [DataField("state")] private string? state;
        [DataField("texture")] private string? texture;

        [ViewVariables(VVAccess.ReadWrite)]
        public int DrawDepth
        {
            get => _drawDepth;
            set
            {
                _drawDepth = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Visible
        {
            get => _visible;
            set
            {
                _visible = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public Angle Rotation
        {
            get => _rotation;
            set
            {
                _rotation = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Directional
        {
            get => _directional;
            set
            {
                _directional = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public string? BaseRSIPath
        {
            get => _baseRSIPath;
            set
            {
                _baseRSIPath = value;
                Dirty();
            }
        }

        public uint _renderOrder;
        [ViewVariables(VVAccess.ReadWrite)]
        public uint RenderOrder
        {
            get => _renderOrder;
            set
            {
                _renderOrder = value;
                Dirty();
            }
        }

        [ViewVariables]
        public int LayerCount => Layers.Count;

        public void AfterDeserialization()
        {
            if (Layers.Count == 0)
            {
                if (state != null || texture != null)
                {
                    var layerZeroData = SharedSpriteComponent.PrototypeLayerData.New();
                    if (!string.IsNullOrWhiteSpace(state))
                    {
                        layerZeroData.State = state;
                    }

                    if (!string.IsNullOrWhiteSpace(texture))
                    {
                        layerZeroData.TexturePath = texture;
                    }

                    Layers.Insert(0, layerZeroData);

                    state = null;
                    texture = null;
                }
            }
        }

        public int AddLayerWithSprite(SpriteSpecifier specifier)
        {
            var layer = PrototypeLayerData.New();
            switch (specifier)
            {
                case SpriteSpecifier.Texture tex:
                    layer.TexturePath = tex.TexturePath.ToString();
                    break;
                case SpriteSpecifier.Rsi rsi:
                    layer.RsiPath = rsi.RsiPath.ToString();
                    layer.State = rsi.RsiState;
                    break;
                default:
                    throw new NotImplementedException();
            }

            Layers.Add(layer);
            Dirty();
            return Layers.Count - 1;
        }

        public int AddLayerWithTexture(string texture)
        {
            var layer = PrototypeLayerData.New();
            layer.TexturePath = texture;
            Layers.Add(layer);
            Dirty();
            return Layers.Count - 1;
        }

        public int AddLayerWithTexture(ResourcePath texture)
        {
            return AddLayerWithTexture(texture.ToString());
        }

        public int AddLayerWithState(string stateId)
        {
            var layer = PrototypeLayerData.New();
            layer.State = stateId;
            Layers.Add(layer);
            Dirty();
            return Layers.Count - 1;
        }

        public int AddLayerWithState(string stateId, string rsiPath)
        {
            var layer = PrototypeLayerData.New();
            layer.State = stateId;
            layer.RsiPath = rsiPath;
            Layers.Add(layer);
            Dirty();
            return Layers.Count - 1;
        }

        public int AddLayerWithState(string stateId, ResourcePath rsiPath)
        {
            return AddLayerWithState(stateId, rsiPath.ToString());
        }

        public void RemoveLayer(int layer)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot remove! Trace:\n{1}",
                    layer, Environment.StackTrace);
                return;
            }

            Layers.RemoveAt(layer);
            Dirty();
        }

        public void LayerSetShader(int layer, string shaderName)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot set shader! Trace:\n{1}",
                    layer, Environment.StackTrace);
                return;
            }

            var thelayer = Layers[layer];
            thelayer.Shader = shaderName;
            Layers[layer] = thelayer;
            Dirty();
        }

        public void LayerSetSprite(int layer, SpriteSpecifier specifier)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot set sprite! Trace:\n{1}",
                    layer, Environment.StackTrace);
                return;
            }

            var thelayer = Layers[layer];
            switch (specifier)
            {
                case SpriteSpecifier.Texture tex:
                    thelayer.TexturePath = tex.TexturePath.ToString();
                    thelayer.RsiPath = null;
                    thelayer.State = null;
                    break;
                case SpriteSpecifier.Rsi rsi:
                    thelayer.TexturePath = null;
                    thelayer.RsiPath = rsi.RsiPath.ToString();
                    thelayer.State = rsi.RsiState;
                    break;
                default:
                    throw new NotImplementedException();
            }

            Layers[layer] = thelayer;
            Dirty();
        }

        public void LayerSetTexture(int layer, string texturePath)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS("go.comp.sprite",
                    "Layer with index '{0}' does not exist, cannot set texture! Trace:\n{1}", layer,
                    Environment.StackTrace);
                return;
            }

            var thelayer = Layers[layer];
            thelayer.State = null;
            thelayer.TexturePath = texturePath;
            Layers[layer] = thelayer;
            Dirty();
        }

        public void LayerSetTexture(int layer, ResourcePath texturePath)
        {
            LayerSetTexture(layer, texturePath.ToString());
        }

        public void LayerSetState(int layer, string stateId)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot set state! Trace:\n{1}",
                    layer, Environment.StackTrace);
                return;
            }

            var thelayer = Layers[layer];
            thelayer.State = stateId;
            thelayer.TexturePath = null;
            Layers[layer] = thelayer;
            Dirty();
        }

        public void LayerSetState(int layer, string stateId, string rsiPath)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS("go.comp.sprite",
                    "Layer with index '{0}' does not exist, cannot set state & RSI! Trace:\n{1}", layer,
                    Environment.StackTrace);
                return;
            }

            var thelayer = Layers[layer];
            thelayer.RsiPath = rsiPath;
            thelayer.State = stateId;
            thelayer.TexturePath = null;
            Layers[layer] = thelayer;
            Dirty();
        }

        public void LayerSetState(int layer, string stateId, ResourcePath rsiPath)
        {
            LayerSetState(layer, stateId, rsiPath.ToString());
        }

        public void LayerSetRSI(int layer, string rsiPath)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot set RSI! Trace:\n{1}",
                    layer, Environment.StackTrace);
                return;
            }

            var thelayer = Layers[layer];
            thelayer.RsiPath = rsiPath;
            Layers[layer] = thelayer;
            Dirty();
        }

        public void LayerSetRSI(int layer, ResourcePath rsiPath)
        {
            LayerSetRSI(layer, rsiPath.ToString());
        }

        public void LayerSetScale(int layer, Vector2 scale)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot set scale! Trace:\n{1}",
                    layer, Environment.StackTrace);
                return;
            }

            var thelayer = Layers[layer];
            thelayer.Scale = scale;
            Layers[layer] = thelayer;
            Dirty();
        }

        public void LayerSetRotation(int layer, Angle rotation)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS("go.comp.sprite",
                    "Layer with index '{0}' does not exist, cannot set rotation! Trace:\n{1}", layer,
                    Environment.StackTrace);
                return;
            }

            var thelayer = Layers[layer];
            thelayer.Rotation = rotation;
            Layers[layer] = thelayer;
            Dirty();
        }

        public void LayerSetVisible(int layer, bool visible)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS("go.comp.sprite",
                    "Layer with index '{0}' does not exist, cannot set visibility! Trace:\n{1}", layer,
                    Environment.StackTrace);
                return;
            }

            var thelayer = Layers[layer];
            thelayer.Visible = visible;
            Layers[layer] = thelayer;
            Dirty();
        }

        public void LayerSetColor(int layer, Color color)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot set color! Trace:\n{1}",
                    layer, Environment.StackTrace);
                return;
            }

            var thelayer = Layers[layer];
            thelayer.Color = color;
            Layers[layer] = thelayer;
            Dirty();
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new SpriteComponentState(Visible, DrawDepth, Scale, Rotation, Offset, Color,
                BaseRSIPath, Layers, RenderOrder);
        }
    }
}
