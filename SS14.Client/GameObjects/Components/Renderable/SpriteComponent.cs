using SS14.Client.Graphics;
using SS14.Client.Graphics.ClientEye;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Graphics.Shaders;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Client.Utility;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using SS14.Shared.Prototypes;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;
using VS = Godot.VisualServer;

namespace SS14.Client.GameObjects
{
    public sealed class SpriteComponent : Component, ISpriteComponent, IClickTargetComponent
    {
        public override string Name => "Sprite";
        public override uint? NetID => NetIDs.SPRITE;
        public override Type StateType => typeof(SpriteComponentState);

        /// <summary>
        ///     The resource path from which all texture paths are relative to.
        /// </summary>
        public static readonly ResourcePath TextureRoot = new ResourcePath("/Textures");

        private bool _visible = true;
        public bool Visible
        {
            get => _visible;
            set
            {
                value = _visible;
                if (value)
                {
                    RedrawQueued = true;
                }
                else
                {
                    ClearDraw();
                }
            }
        }

        private DrawDepth drawDepth;

        /// <summary>
        ///     Z-index for drawing.
        /// </summary>
        public DrawDepth DrawDepth
        {
            get => drawDepth;
            set
            {
                drawDepth = value;
                if (SceneNode != null)
                {
                    SceneNode.ZIndex = (int)value;
                }
            }
        }

        private Vector2 scale = Vector2.One;
        /// <summary>
        ///     A scale applied to all layers.
        /// </summary>
        public Vector2 Scale
        {
            get => scale;
            set
            {
                scale = value;
                if (SceneNode != null)
                {
                    SceneNode.Scale = value.Convert();
                }
            }
        }

        private Angle rotation;
        public Angle Rotation
        {
            get => rotation;
            set
            {
                rotation = value;
                if (SceneNode != null)
                {
                    SceneNode.Rotation = (float)value;
                }
            }
        }

        private Vector2 offset = Vector2.Zero;
        /// <summary>
        ///     Offset applied to all layers.
        /// </summary>
        public Vector2 Offset
        {
            get => offset;
            set
            {
                offset = value;
                if (SceneNode != null)
                {
                    SceneNode.Position = value.Convert() * EyeManager.PIXELSPERMETER;
                }
            }
        }

        private Color color = Color.White;
        public Color Color
        {
            get => color;
            set
            {
                color = value;
                if (SceneNode != null)
                {
                    SceneNode.Modulate = value.Convert();
                }
            }
        }

        /// <summary>
        ///     Controls whether we use RSI directions to rotate, or just get angular rotation applied.
        ///     If true, all rotation to this sprite component is negated (that is rotation from say the owner being rotated).
        ///     Rotation transformations on individual layers still apply.
        ///     If false, all layers get locked to south and rotation is a transformation.
        /// </summary>
        public bool Directional
        {
            get => _directional;
            set
            {
                _directional = value;
                RedrawQueued = true;
            }
        }

        private bool _directional = true;

        private bool RedrawQueued = true;

        private RSI _baseRsi;
        public RSI BaseRSI
        {
            get => _baseRsi;
            set
            {
                _baseRsi = value;
                for (var i = 0; i < Layers.Count; i++)
                {
                    var layer = Layers[i];
                    if (!layer.State.IsValid || layer.RSI != null)
                    {
                        continue;
                    }

                    if (value.TryGetState(layer.State, out var state))
                    {
                        (layer.Texture, layer.AnimationTimeLeft) = state.GetFrame(0, 0);
                        layer.CurrentDir = RSI.State.Direction.South;
                    }
                    else
                    {
                        Logger.ErrorS("go.comp.sprite",
                                      "Layer '{0}'no longer has state '{1}' due to base RSI change. Trace:\n{2}",
                                      i, layer.State, Environment.StackTrace);
                        layer.Texture = null;
                    }
                }
            }
        }

        private List<Layer> Layers = new List<Layer>();
        private List<Godot.RID> CanvasItems = new List<Godot.RID>();

        private Godot.Node2D SceneNode;
        private IGodotTransformComponent TransformComponent;

        private IResourceCache resourceCache;
        private IPrototypeManager prototypes;

        public int AddLayer(Texture texture)
        {
            var layer = Layer.New();
            layer.Texture = texture;
            Layers.Add(layer);
            RedrawQueued = true;
            return Layers.Count - 1;
        }

        public int AddLayer(RSI.StateId stateId)
        {
            var layer = Layer.New();
            layer.State = stateId;
            if (BaseRSI.TryGetState(stateId, out var state))
            {
                (layer.Texture, layer.AnimationTimeLeft) = state.GetFrame(layer.CurrentDir, 0);
            }
            else
            {
                Logger.ErrorS("go.comp.sprite", "State does not exist in RSI: '{0}'. Trace:\n{1}", stateId, Environment.StackTrace);
            }
            Layers.Add(layer);
            RedrawQueued = true;
            return Layers.Count - 1;
        }

        public int AddLayer(RSI.StateId stateId, RSI rsi)
        {
            var layer = Layer.New();
            layer.State = stateId;
            layer.RSI = rsi;
            if (rsi.TryGetState(stateId, out var state))
            {
                (layer.Texture, layer.AnimationTimeLeft) = state.GetFrame(layer.CurrentDir, 0);
            }
            else
            {
                Logger.ErrorS("go.comp.sprite", "State does not exist in RSI: '{0}'. Trace:\n{1}", stateId, Environment.StackTrace);
            }
            Layers.Add(layer);
            RedrawQueued = true;
            return Layers.Count - 1;
        }

        public void RemoveLayer(int layer)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot remove! Trace:\n{1}", layer, Environment.StackTrace);
                return;
            }
            Layers.RemoveAt(layer);
            RedrawQueued = true;
        }

        public void LayerSetShader(int layer, Shader shader)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot set shader! Trace:\n{1}", layer, Environment.StackTrace);
                return;
            }
            var thelayer = Layers[layer];
            thelayer.Shader = shader;
            Layers[layer] = thelayer;
            RedrawQueued = true;
        }

        public void LayerSetShader(int layer, string shaderName)
        {
            if (!prototypes.TryIndex<ShaderPrototype>(shaderName, out var prototype))
            {
                Logger.ErrorS("go.comp.sprite", "Shader prototype '{0}' does not exist. Trace:\n{1}", shaderName, Environment.StackTrace);
            }

            // This will set the shader to null if it does not exist.
            LayerSetShader(layer, prototype?.Instance());
        }

        public void LayerSetTexture(int layer, Texture texture)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot set texture! Trace:\n{1}", layer, Environment.StackTrace);
                return;
            }
            var thelayer = Layers[layer];
            if (thelayer.State.IsValid)
            {
                Logger.ErrorS("go.comp.sprite", "Cannot change texture of a layer with an RSI State. Trace:\n{0}", Environment.StackTrace);
                return;
            }
            thelayer.Texture = texture;
            Layers[layer] = thelayer;
            RedrawQueued = true;
        }

        public void LayerSetTexture(int layer, string texturePath)
        {
            LayerSetTexture(layer, new ResourcePath(texturePath));
        }

        public void LayerSetTexture(int layer, ResourcePath texturePath)
        {
            if (!resourceCache.TryGetResource<TextureResource>(TextureRoot / texturePath, out var texture))
            {
                Logger.ErrorS("go.comp.sprite", "Unable to load texture '{0}'. Trace:\n{1}", texturePath, Environment.StackTrace);
            }

            LayerSetTexture(layer, texture);
        }

        public void LayerSetState(int layer, RSI.StateId stateId)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot set state! Trace:\n{1}", layer, Environment.StackTrace);
                return;
            }

            var thelayer = Layers[layer];
            thelayer.State = stateId;
            var rsi = thelayer.RSI ?? BaseRSI;
            if (rsi == null)
            {
                Logger.ErrorS("go.comp.sprite", "No RSI to pull new state from! Trace:\n{1}", Environment.StackTrace);
                thelayer.Texture = null;
            }
            else
            {
                if (rsi.TryGetState(stateId, out var state))
                {
                    (thelayer.Texture, thelayer.AnimationTimeLeft) = state.GetFrame(0, 0);
                    thelayer.CurrentDir = RSI.State.Direction.South;
                }
                else
                {
                    Logger.ErrorS("go.comp.sprite", "State '{0}' does not exist in RSI. Trace:\n{1}", stateId, Environment.StackTrace);
                    thelayer.Texture = null;
                }
            }

            Layers[layer] = thelayer;
            RedrawQueued = true;
        }

        public void LayerSetState(int layer, RSI.StateId stateId, RSI rsi)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot set state! Trace:\n{1}", layer, Environment.StackTrace);
                return;
            }

            var thelayer = Layers[layer];
            thelayer.State = stateId;
            thelayer.RSI = rsi;
            var actualrsi = thelayer.RSI ?? BaseRSI;
            if (actualrsi == null)
            {
                Logger.ErrorS("go.comp.sprite", "No RSI to pull new state from! Trace:\n{1}", layer, Environment.StackTrace);
                thelayer.Texture = null;
            }
            else
            {
                if (actualrsi.TryGetState(stateId, out var state))
                {
                    (thelayer.Texture, thelayer.AnimationTimeLeft) = state.GetFrame(0, 0);
                    thelayer.CurrentDir = RSI.State.Direction.South;
                }
                else
                {
                    Logger.ErrorS("go.comp.sprite", "State '{0}' does not exist in RSI. Trace:\n{1}", stateId, Environment.StackTrace);
                    thelayer.Texture = null;
                }
            }

            Layers[layer] = thelayer;
            RedrawQueued = true;
        }

        public void LayerSetState(int layer, RSI.StateId stateId, string rsiPath)
        {
            LayerSetState(layer, stateId, new ResourcePath(rsiPath));
        }

        public void LayerSetState(int layer, RSI.StateId stateId, ResourcePath rsiPath)
        {
            if (!resourceCache.TryGetResource<RSIResource>(TextureRoot / rsiPath, out var res))
            {
                Logger.ErrorS("go.comp.sprite", "Unable to load RSI '{0}'. Trace:\n{1}", rsiPath, Environment.StackTrace);
            }

            LayerSetState(layer, stateId, res?.RSI);
        }

        public void LayerSetRSI(int layer, RSI rsi)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS("go.comp.sprite", "Layer with index '{0}' does not exist, cannot set RSI! Trace:\n{1}", layer, Environment.StackTrace);
                return;
            }

            var thelayer = Layers[layer];
            thelayer.RSI = rsi;
            if (!thelayer.State.IsValid)
            {
                Layers[layer] = thelayer;
                return;
            }

            // Gotta do this because somebody might use null as argument (totally valid).
            var actualRsi = thelayer.RSI ?? BaseRSI;
            if (actualRsi == null)
            {
                Logger.ErrorS("go.comp.sprite", "No RSI to pull new state from! Trace:\n{1}", layer, Environment.StackTrace);
                thelayer.Texture = null;
            }
            else
            {
                if (rsi.TryGetState(thelayer.State, out var state))
                {
                    (thelayer.Texture, thelayer.AnimationTimeLeft) = state.GetFrame(0, 0);
                    thelayer.CurrentDir = RSI.State.Direction.South;
                }
                else
                {
                    Logger.ErrorS("go.comp.sprite", "State '{0}' does not exist in set RSI. Trace:\n{1}", thelayer.State, Environment.StackTrace);
                    thelayer.Texture = null;
                }
            }

            Layers[layer] = thelayer;
            RedrawQueued = true;
        }

        public void LayerSetRSI(int layer, string rsiPath)
        {
            LayerSetRSI(layer, new ResourcePath(rsiPath));
        }

        public void LayerSetRSI(int layer, ResourcePath rsiPath)
        {
            if (!resourceCache.TryGetResource<RSIResource>(TextureRoot / rsiPath, out var res))
            {
                Logger.ErrorS("go.comp.sprite", "Unable to load RSI '{0}'. Trace:\n{1}", rsiPath, Environment.StackTrace);
            }

            LayerSetRSI(layer, res?.RSI);
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
            RedrawQueued = true;
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
            RedrawQueued = true;
        }

        public override void OnAdd()
        {
            base.OnAdd();
            SceneNode = new Godot.Node2D()
            {
                Name = "Sprite",
                ZIndex = (int)drawDepth,
                Scale = scale.Convert(),
                Position = offset.Convert(),
                Modulate = color.Convert(),
                Rotation = (float)rotation,
            };
        }

        public override void OnRemove()
        {
            base.OnRemove();

            ClearDraw();
            SceneNode.QueueFree();
        }

        public override void Initialize()
        {
            base.Initialize();

            TransformComponent = Owner.GetComponent<IGodotTransformComponent>();
            TransformComponent.SceneNode.AddChild(SceneNode);
            Redraw();
        }

        private void ClearDraw()
        {
            foreach (var item in CanvasItems)
            {
                VS.FreeRid(item);
            }
            CanvasItems.Clear();
        }

        private void Redraw()
        {
            ClearDraw();

            if (!Visible)
            {
                return;
            }

            Shader prevShader = null;
            Godot.RID currentItem = null;
            foreach (var layer in Layers)
            {
                var shader = layer.Shader;
                var texture = layer.Texture ?? resourceCache.GetFallback<TextureResource>();
                if (currentItem == null || prevShader != shader)
                {
                    currentItem = VS.CanvasItemCreate();
                    VS.CanvasItemSetParent(currentItem, SceneNode.GetCanvasItem());
                    CanvasItems.Add(currentItem);
                    if (shader != null)
                    {
                        VS.CanvasItemSetMaterial(currentItem, shader.GodotMaterial.GetRid());
                    }
                    prevShader = shader;
                }

                var transform = Godot.Transform2D.Identity;
                DrawingHandle.SetTransform2DRotationAndScale(ref transform, layer.Rotation, layer.Scale);
                VS.CanvasItemAddSetTransform(currentItem, transform);
                // Not instantiating a DrawingHandle here because those are ref types,
                // and I really don't want the extra allocation.
                texture.GodotTexture.Draw(currentItem, -texture.GodotTexture.GetSize() / 2);
            }
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            base.LoadParameters(mapping);

            prototypes = IoCManager.Resolve<IPrototypeManager>();
            resourceCache = IoCManager.Resolve<IResourceCache>();

            if (mapping.TryGetNode("sprite", out YamlNode node))
            {
                var path = TextureRoot / node.AsResourcePath();
                try
                {
                    BaseRSI = resourceCache.GetResource<RSIResource>(path).RSI;
                }
                catch
                {
                    Logger.ErrorS("go.comp.sprite", "Unable to load RSI '{0}'. Prototype: '{1}'", path, Owner.Prototype.ID);
                }
            }

            if (mapping.TryGetNode("state", out node))
            {
                if (BaseRSI == null)
                {
                    Logger.ErrorS("go.comp.sprite",
                                  "No base RSI set to load states from: "
                                  + "cannot use 'state' property. Prototype: '{0}'", Owner.Prototype.ID);
                }
                else
                {
                    var stateid = new RSI.StateId(node.AsString(), RSI.Selectors.None);
                    var layer = Layer.New();
                    layer.State = stateid;

                    if (BaseRSI.TryGetState(stateid, out var state))
                    {
                        layer.Texture = state.GetFrame(layer.CurrentDir, 0).icon;
                    }
                    else
                    {
                        Logger.ErrorS("go.comp.sprite",
                                      "State not found in RSI: '{0}'. Prototype: '{1}'",
                                      stateid, Owner.Prototype.ID);
                    }

                    Layers.Add(layer);
                }
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
                    var path = node.AsResourcePath();
                    var layer = Layer.New();
                    layer.Texture = resourceCache.GetResource<TextureResource>(TextureRoot / path);
                    Layers.Add(layer);
                }
            }

            if (mapping.TryGetNode("scale", out node))
            {
                Scale = node.AsVector2();
            }

            if (mapping.TryGetNode("rotation", out node))
            {
                Rotation = Angle.FromDegrees(node.AsFloat());
            }

            if (mapping.TryGetNode("offset", out node))
            {
                Offset = node.AsVector2();
            }

            if (mapping.TryGetNode("drawdepth", out node))
            {
                DrawDepth = node.AsEnum<DrawDepth>();
            }

            if (mapping.TryGetNode("color", out node))
            {
                Color = node.AsColor();
            }

            if (mapping.TryGetNode("directional", out node))
            {
                Directional = node.AsBool();
            }

            if (mapping.TryGetNode("visible", out node))
            {
                Visible = node.AsBool();
            }

            if (mapping.TryGetNode("layers", out YamlSequenceNode layers))
            {
                foreach (var layernode in layers.Cast<YamlMappingNode>())
                {
                    LoadLayerFrom(layernode, prototypes);
                }
            }
        }

        private void LoadLayerFrom(YamlMappingNode mapping, IPrototypeManager prototypeManager)
        {
            var layer = Layer.New();

            if (mapping.TryGetNode("sprite", out var node))
            {
                var path = TextureRoot / node.AsResourcePath();
                try
                {
                    layer.RSI = resourceCache.GetResource<RSIResource>(path).RSI;
                }
                catch
                {
                    Logger.ErrorS("go.comp.sprite", "Unable to load layer RSI '{0}'. Prototype: '{1}'", path, Owner.Prototype.ID);
                }
            }

            if (mapping.TryGetNode("state", out node))
            {
                var rsi = layer.RSI ?? BaseRSI;
                if (rsi == null)
                {
                    Logger.ErrorS("go.comp.sprite",
                                  "Layer has no RSI to load states from."
                                  + "cannot use 'state' property. Prototype: '{0}'", Owner.Prototype.ID);
                }
                else
                {
                    var stateid = new RSI.StateId(node.AsString(), RSI.Selectors.None);
                    layer.State = stateid;
                    if (BaseRSI.TryGetState(stateid, out var state))
                    {
                        layer.Texture = state.GetFrame(layer.CurrentDir, 0).icon;
                    }
                    else
                    {
                        Logger.ErrorS("go.comp.sprite",
                                      "State not found in layer RSI: '{0}'. Prototype: '{1}'",
                                      stateid, Owner.Prototype.ID);
                    }
                }
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
                    var path = node.AsResourcePath();
                    layer.Texture = resourceCache.GetResource<TextureResource>(TextureRoot / path);
                }
            }

            if (mapping.TryGetNode("shader", out node))
            {
                if (prototypeManager.TryIndex<ShaderPrototype>(node.AsString(), out var prototype))
                {
                    layer.Shader = prototype.Instance();
                }
                else
                {
                    Logger.ErrorS("go.comp.sprite",
                                  "Shader prototype '{0}' does not exist. Prototype: '{1}'",
                                  node.AsString(), Owner.Prototype.ID);
                }
            }

            if (mapping.TryGetNode("scale", out node))
            {
                layer.Scale = node.AsVector2();
            }

            if (mapping.TryGetNode("rotation", out node))
            {
                layer.Rotation = Angle.FromDegrees(node.AsFloat());
            }

            Layers.Add(layer);
        }

        public void FrameUpdate(float delta)
        {
            if (Directional)
            {
                SceneNode.Rotation = (float)(-TransformComponent.WorldRotation + MathHelper.PiOver2);
            }

            for (var i = 0; i < Layers.Count; i++)
            {
                var layer = Layers[i];
                // Since State is a struct, we can't null-check it directly.
                if (!layer.State.IsValid)
                {
                    continue;
                }

                var state = (layer.RSI ?? BaseRSI)[layer.State];
                RSI.State.Direction dir;
                if (!Directional || state.Directions == RSI.State.DirectionType.Dir1)
                {
                    dir = RSI.State.Direction.South;
                }
                else
                {
                    dir = GetDir();
                }
                if (dir == layer.CurrentDir)
                {
                    var delayCount = state.DelayCount(dir);
                    if (delayCount < 2)
                    {
                        // Don't bother animating this.
                        // There's no animation frames!
                        continue;
                    }
                    layer.AnimationTimeLeft -= delta;
                    while (layer.AnimationTimeLeft < 0)
                    {
                        if (++layer.AnimationFrame >= delayCount)
                        {
                            layer.AnimationFrame = 0;
                        }
                        layer.AnimationTimeLeft += state.GetFrame(dir, layer.AnimationFrame).delay;
                    }
                    layer.Texture = state.GetFrame(dir, layer.AnimationFrame).icon;
                    RedrawQueued = true;
                }
                else
                {
                    // For simplicity, turning causes the animation to reset FOR NOW.
                    // This might be changed.
                    // Not sure how you'd go about it.
                    layer.CurrentDir = dir;
                    layer.AnimationFrame = 0;
                    (layer.Texture, layer.AnimationTimeLeft) = state.GetFrame(dir, 0);
                    RedrawQueued = true;
                }
                Layers[i] = layer;
            }

            if (RedrawQueued)
            {
                Redraw();
                RedrawQueued = false;
            }
        }

        private RSI.State.Direction GetDir()
        {
            var angle = new Angle(-TransformComponent.WorldRotation);
            return angle.GetDir().Convert();
        }

        private struct Layer
        {
            public Shader Shader;
            public Texture Texture;

            public RSI RSI;
            public RSI.StateId State;
            public RSI.State.Direction CurrentDir;
            public float AnimationTimeLeft;
            public int AnimationFrame;
            public Vector2 Scale;
            public Angle Rotation;

            public static Layer New()
            {
                return new Layer()
                {
                    CurrentDir = RSI.State.Direction.South,
                    Scale = Vector2.One,
                };
            }
        }
    }
}
