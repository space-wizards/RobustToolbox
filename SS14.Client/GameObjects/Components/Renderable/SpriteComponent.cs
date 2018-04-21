using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Client.Graphics;
using SS14.Client.Graphics.ClientEye;
using SS14.Client.Graphics.Shaders;
using SS14.Client.Interfaces;
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

        RSI ISpriteComponent.BaseRSI { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private bool _directional = true;

        private bool RedrawQueued = true;

        private RSI BaseRSI;

        private List<Layer> Layers = new List<Layer>();
        private List<Godot.RID> CanvasItems = new List<Godot.RID>();

        private Godot.Node2D SceneNode;
        private IGodotTransformComponent TransformComponent;

        private IResourceCache resourceCache = IoCManager.Resolve<IResourceCache>();


        public int AddLayer(Texture texture)
        {
            var layer = Layer.New();
            layer.Texture = texture;
            Layers.Add(layer);
            return Layers.Count - 1;
        }

        public int AddLayer(RSI.StateId stateId, RSI rsi = null)
        {
        }

        public void RemoveLayer(int layer)
        {
            throw new NotImplementedException();
        }

        public void LayerSetShader(int layer, Shader shader)
        {
            throw new NotImplementedException();
        }

        public void LayerSetTexture(int layer, Texture texture)
        {
            throw new NotImplementedException();
        }

        public void LayerSetState(int layer, RSI.StateId stateId, RSI rsi = null)
        {
            throw new NotImplementedException();
        }

        public void LayerSetRSI(int layer, RSI rsi)
        {
            throw new NotImplementedException();
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

                VS.CanvasItemAddSetTransform(currentItem, layer.Transform);
                // Not instantiating a DrawingHandle here because those are ref types,
                // and I really don't want the extra allocation.
                texture.GodotTexture.Draw(currentItem, -texture.GodotTexture.GetSize() / 2);
            }
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            base.LoadParameters(mapping);

            var prototypes = IoCManager.Resolve<IPrototypeManager>();

            YamlNode node;
            if (mapping.TryGetNode("sprite", out node))
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
                    var layer = new Layer
                    {
                        State = stateid,
                        Transform = Godot.Transform2D.Identity,
                    };

                    if (BaseRSI.TryGetState(stateid, out var state))
                    {
                        layer.Texture = state.GetFrame(RSI.State.Direction.South, 0).icon;
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
                        layer.Texture = state.GetFrame(RSI.State.Direction.South, 0).icon;
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
                layer.Transform = Godot.Transform2D.Identity.Scaled(node.AsVector2().Convert());
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
            public Godot.Transform2D Transform;

            public static Layer New()
            {
                return new Layer()
                {
                    Transform = Godot.Transform2D.Identity,
                };
            }
        }
    }
}
