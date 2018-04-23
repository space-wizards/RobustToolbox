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

        // So because the game state system is HOT GARBAGE we can't just spam states at the client.
        // This kills the client.
        // So we basically pre-implement a dirty system.
        // We increase this gen for every change.
        // If it's different the client actually gives a shit about our component state.
        // Else it just ignores it and mourns due to the lost CPU time from bsdiff.
        // NOTE: Generation is NOT updated by initial data load. Everything's from the prototype. That's fine.
        // This means the client ignores us until there's a *difference*.
        private int generation = 0;

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
                generation++;
            }
        }

        public bool Visible
        {
            get => _visible;
            set
            {
                _visible = value;
                generation++;
            }
        }

        public Vector2 Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                generation++;
            }
        }

        public Angle Rotation
        {
            get => _rotation;
            set
            {
                _rotation = value;
                generation++;
            }
        }

        public Vector2 Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                generation++;
            }
        }

        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                generation++;
            }
        }

        public bool Directional
        {
            get => _directional;
            set
            {
                _directional = value;
                generation++;
            }
        }

        public string BaseRSIPath
        {
            get => _baseRSIPath;
            set
            {
                _baseRSIPath = value;
                generation++;
            }
        }

        int AddLayerWithTexture(string texture)
        {
            var layer = Layer.New();
            layer.TexturePath = texture;
            Layers.Add(layer);
            generation++;
            return Layers.Count - 1;
        }

        int AddLayerWithTexture(ResourcePath texture)
        {
            return AddLayerWithTexture(texture.ToString());
        }

        int AddLayerWithState(string stateId)
        {
            var layer = Layer.New();
            layer.State = stateId;
            Layers.Add(layer);
            generation++;
            return Layers.Count - 1;
        }

        int AddLayerWithState(string stateId, string rsiPath)
        {
            var layer = Layer.New();
            layer.State = stateId;
            layer.RsiPath = rsiPath;
            Layers.Add(layer);
            generation++;
            return Layers.Count - 1;

        }
        int AddLayerWithState(string stateId, ResourcePath rsiPath)
        {
            return AddLayerWithState(stateId, rsiPath.ToString());
        }

        void RemoveLayer(int layer)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot remove! Trace:\n{1}", layer, Environment.StackTrace);
                return;
            }
            Layers.RemoveAt(layer);
            generation++;
        }

        void LayerSetShader(int layer, string shaderName)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot set shader! Trace:\n{1}", layer, Environment.StackTrace);
                return;
            }
            var thelayer = Layers[layer];
            thelayer.Shader = shaderName;
            Layers[layer] = thelayer;
            generation++;
        }

        void LayerSetTexture(int layer, string texturePath)
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
            generation++;
        }
        void LayerSetTexture(int layer, ResourcePath texturePath)
        {
            LayerSetTexture(layer, texturePath.ToString());
        }

        void LayerSetState(int layer, string stateId)
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
            generation++;
        }

        void LayerSetState(int layer, string stateId, string rsiPath)
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
            generation++;
        }

        void LayerSetState(int layer, string stateId, ResourcePath rsiPath)
        {
            LayerSetState(layer, stateId, rsiPath.ToString());
        }

        void LayerSetRSI(int layer, string rsiPath)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot set RSI! Trace:\n{1}", layer, Environment.StackTrace);
                return;
            }

            var thelayer = Layers[layer];
            thelayer.RsiPath = rsiPath;
            Layers[layer] = thelayer;
            generation++;
        }

        void LayerSetRSI(int layer, ResourcePath rsiPath)
        {
            LayerSetRSI(layer, rsiPath.ToString());
        }

        void LayerSetScale(int layer, Vector2 scale)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot set scale! Trace:\n{1}", layer, Environment.StackTrace);
                return;
            }

            var thelayer = Layers[layer];
            thelayer.Scale = scale;
            Layers[layer] = thelayer;
            generation++;
        }

        void LayerSetRotation(int layer, Angle rotation)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot set rotation! Trace:\n{1}", layer, Environment.StackTrace);
                return;
            }

            var thelayer = Layers[layer];
            thelayer.Rotation = rotation;
            Layers[layer] = thelayer;
            generation++;
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

            Layers.Add(layer);
        }

        public override ComponentState GetComponentState()
        {
            return new SpriteComponentState(generation, Visible, DrawDepth, Scale, Rotation, Offset, Color, Directional, BaseRSIPath, null);
        }

        private struct Layer
        {
            public string Shader;
            public string TexturePath;
            public string RsiPath;
            public string State;
            public Vector2 Scale;
            public Angle Rotation;

            public static Layer New()
            {
                return new Layer
                {
                    Scale = Vector2.One
                };
            }
        }
    }
}
