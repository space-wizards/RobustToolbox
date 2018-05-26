using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Serialization;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace SS14.Server.GameObjects
{
    public class SpriteComponent : Component, ISpriteRenderableComponent
    {
        public override string Name => "Sprite";
        public override uint? NetID => NetIDs.SPRITE;

        private List<Layer> Layers = new List<Layer>();

        private bool _visible;
        private DrawDepth _drawDepth;
        private Vector2 _scale;
        private Vector2 _offset;
        private Color _color;
        private bool _directional;
        private string _baseRSIPath;
        private Angle _rotation;

        public DrawDepth DrawDepth
        {
            get => _drawDepth;
            set
            {
                _drawDepth = value;
                Dirty();
            }
        }

        public bool Visible
        {
            get => _visible;
            set
            {
                _visible = value;
                Dirty();
            }
        }

        public Vector2 Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                Dirty();
            }
        }

        public Angle Rotation
        {
            get => _rotation;
            set
            {
                _rotation = value;
                Dirty();
            }
        }

        public Vector2 Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                Dirty();
            }
        }

        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                Dirty();
            }
        }

        public bool Directional
        {
            get => _directional;
            set
            {
                _directional = value;
                Dirty();
            }
        }

        public string BaseRSIPath
        {
            get => _baseRSIPath;
            set
            {
                _baseRSIPath = value;
                Dirty();
            }
        }

        public int AddLayerWithTexture(string texture)
        {
            var layer = Layer.New();
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
            var layer = Layer.New();
            layer.State = stateId;
            Layers.Add(layer);
            Dirty();
            return Layers.Count - 1;
        }

        public int AddLayerWithState(string stateId, string rsiPath)
        {
            var layer = Layer.New();
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
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot remove! Trace:\n{1}", layer, Environment.StackTrace);
                return;
            }
            Layers.RemoveAt(layer);
            Dirty();
        }

        public void LayerSetShader(int layer, string shaderName)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot set shader! Trace:\n{1}", layer, Environment.StackTrace);
                return;
            }
            var thelayer = Layers[layer];
            thelayer.Shader = shaderName;
            Layers[layer] = thelayer;
            Dirty();
        }

        public void LayerSetTexture(int layer, string texturePath)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot set texture! Trace:\n{1}", layer, Environment.StackTrace);
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
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot set set! Trace:\n{1}", layer, Environment.StackTrace);
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
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot set state & RSI! Trace:\n{1}", layer, Environment.StackTrace);
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
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot set RSI! Trace:\n{1}", layer, Environment.StackTrace);
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
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot set scale! Trace:\n{1}", layer, Environment.StackTrace);
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
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot set rotation! Trace:\n{1}", layer, Environment.StackTrace);
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
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot set visibility! Trace:\n{1}", layer, Environment.StackTrace);
                return;
            }

            var thelayer = Layers[layer];
            thelayer.Visible = visible;
            Layers[layer] = thelayer;
            Dirty();
        }

        public override void ExposeData(EntitySerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _visible, "visible", true);
            serializer.DataField(ref _drawDepth, "depth", DrawDepth.FloorTiles);
            serializer.DataField(ref _offset, "offset", Vector2.Zero);
            serializer.DataField(ref _scale, "scale", Vector2.One);
            serializer.DataField(ref _color, "color", Color.White);
            serializer.DataField(ref _directional, "directional", true);
            serializer.DataField(ref _baseRSIPath, "sprite", null);
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            base.LoadParameters(mapping);

            if (mapping.TryGetNode("rotation", out var node))
            {
                Rotation = Angle.FromDegrees(node.AsFloat());
            }

            if (mapping.TryGetNode("sprite", out node))
            {
                BaseRSIPath = node.AsString();
            }

            if (mapping.TryGetNode("texture", out node))
            {
                if (mapping.HasNode("state"))
                {
                    Logger.ErrorS("go.comp.sprite",
                                  "Cannot use 'texture' if an RSI state is provided. Prototype: '{0}'",
                                  Owner.Prototype.ID);
                }
                else
                {
                    var layer = Layer.New();
                    layer.TexturePath = node.AsString();
                    Layers.Add(layer);
                }
            }

            if (mapping.TryGetNode("state", out node))
            {
                if (BaseRSIPath == null)
                {
                    Logger.ErrorS("go.comp.sprite",
                                  "No base RSI set to load states from: "
                                  + "cannot use 'state' property. Prototype: '{0}'", Owner.Prototype.ID);
                }
                else
                {
                    var layer = Layer.New();
                    layer.State = node.AsString();
                    Layers.Add(layer);
                }
            }

            if (mapping.TryGetNode("layers", out YamlSequenceNode layers))
            {
                foreach (var layernode in layers.Cast<YamlMappingNode>())
                {
                    LoadLayerFrom(layernode);
                }
            }
        }

        private void LoadLayerFrom(YamlMappingNode mapping)
        {
            var layer = Layer.New();

            if (mapping.TryGetNode("sprite", out var node))
            {
                layer.RsiPath = node.AsString();
            }

            if (mapping.TryGetNode("state", out node))
            {
                layer.State = node.AsString();
            }

            if (mapping.TryGetNode("texture", out node))
            {
                if (layer.State != null)
                {
                    Logger.ErrorS("go.comp.sprite",
                                  "Cannot specify 'texture' on a layer if it has an RSI state specified. Prototype: '{0}'",
                                  Owner.Prototype.ID);
                }
                else
                {
                    layer.TexturePath = node.AsString();
                }
            }

            if (mapping.TryGetNode("shader", out node))
            {
                layer.Shader = node.AsString();
            }

            if (mapping.TryGetNode("scale", out node))
            {
                layer.Scale = node.AsVector2();
            }

            if (mapping.TryGetNode("rotation", out node))
            {
                layer.Rotation = node.AsFloat();
            }

            if (mapping.TryGetNode("visible", out node))
            {
                layer.Visible = node.AsBool();
            }

            Layers.Add(layer);
        }

        public override ComponentState GetComponentState()
        {
            var list = Layers.Select((l) => l.ToStateLayer()).ToList();
            return new SpriteComponentState(Visible, DrawDepth, Scale, Rotation, Offset, Color, Directional, BaseRSIPath, list);
        }
        private struct Layer
        {
            public string Shader;
            public string TexturePath;
            public string RsiPath;
            public string State;
            public Vector2 Scale;
            public Angle Rotation;
            public bool Visible;

            public static Layer New()
            {
                return new Layer
                {
                    Scale = Vector2.One,
                    Visible = true,
                };
            }

            public SpriteComponentState.Layer ToStateLayer()
            {
                return new SpriteComponentState.Layer(Shader, TexturePath, RsiPath, State, Scale, Rotation, Visible);
            }
        }
    }
}
