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

        /// <summary>
        ///     A scale applied to all layers.
        /// </summary>
        private Vector2 scale = Vector2.One;
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

        private RSI BaseRSI;

        private List<Layer> Layers = new List<Layer>();
        private List<Godot.RID> CanvasItems = new List<Godot.RID>();

        private Godot.Node2D SceneNode;
        private IGodotTransformComponent TransformComponent;

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
                var texture = layer.Texture;
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
                texture.GodotTexture.Draw(currentItem, -texture.GodotTexture.GetSize() / 2);
            }
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            base.LoadParameters(mapping);

            var resc = IoCManager.Resolve<IResourceCache>();
            var prototypes = IoCManager.Resolve<IPrototypeManager>();

            YamlNode node;
            if (mapping.TryGetNode("sprite", out node))
            {
                var path = node.AsResourcePath();
                var rsi = resc.GetResource<RSIResource>(TextureRoot / path);
                BaseRSI = rsi.RSI;
            }

            if (mapping.TryGetNode("state", out node))
            {
                if (BaseRSI == null)
                {
                    throw new InvalidOperationException("Must have an RSI set to use states.");
                }
                var state = new RSI.StateId(node.AsString(), RSI.Selectors.None);
                var layer = new Layer
                {
                    State = state,
                    Texture = BaseRSI[state].GetFrame(0, 0).icon,
                    Transform = Godot.Transform2D.Identity,
                };
                Layers.Add(layer);
            }

            if (mapping.TryGetNode("texture", out node))
            {
                if (BaseRSI != null)
                {
                    throw new InvalidOperationException("Cannot have both a texture and an RSI specified in prototype!");
                }
                var path = node.AsResourcePath();
                var tex = resc.GetResource<TextureResource>(TextureRoot / path);
                var layer = new Layer
                {
                    Texture = tex,
                    Transform = Godot.Transform2D.Identity,
                };
                Layers.Add(layer);
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

            if (mapping.TryGetNode("layers", out YamlSequenceNode layers))
            {
                foreach (var layernode in layers.Cast<YamlMappingNode>())
                {
                    LoadLayerFrom(layernode, resc, prototypes);
                }
            }
        }

        private void LoadLayerFrom(YamlMappingNode mapping, IResourceCache resourceCache, IPrototypeManager prototypeManager)
        {
            var layer = new Layer();

            if (mapping.TryGetNode("sprite", out var node))
            {
                var path = node.AsResourcePath();
                var rsi = resourceCache.GetResource<RSIResource>(TextureRoot / path);
                layer.RSI = rsi.RSI;
            }

            if (mapping.TryGetNode("state", out node))
            {
                var rsi = layer.RSI ?? BaseRSI;
                if (rsi == null)
                {
                    throw new InvalidOperationException("Must have an RSI to pull states from");
                }

                var state = new RSI.StateId(node.AsString(), RSI.Selectors.None);
                layer.State = state;
                layer.Texture = rsi[state].GetFrame(RSI.State.Direction.South, 0).icon;
            }

            if (mapping.TryGetNode("texture", out node))
            {
                if (layer.RSI != null)
                {
                    throw new InvalidOperationException("Cannot have both a texture and an RSI specified in layer!");
                }
                var path = node.AsResourcePath();
                layer.Texture = resourceCache.GetResource<TextureResource>(TextureRoot / path);
            }

            if (mapping.TryGetNode("shader", out node))
            {
                layer.Shader = IoCManager.Resolve<IPrototypeManager>().Index<ShaderPrototype>(node.AsString()).Instance();
            }

            if (mapping.TryGetNode("scale", out node))
            {
                layer.Transform = Godot.Transform2D.Identity.Scaled(node.AsVector2().Convert());
            }
            else
            {
                layer.Transform = Godot.Transform2D.Identity;
            }

            Layers.Add(layer);
        }

        public void FrameUpdate(float delta)
        {
            bool doRedraw = false;

            for (var i = 0; i < Layers.Count; i++)
            {
                var layer = Layers[i];
                if (!layer.State.IsValid)
                {
                    continue;
                }

                // For simplicity, turning causes the animation to reset FOR NOW.
                // This might be changed.
                var state = (layer.RSI ?? BaseRSI)[layer.State];
                RSI.State.Direction dir;
                if (state.Directions == RSI.State.DirectionType.Dir1)
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
                    doRedraw = true;
                }
                else
                {
                    layer.CurrentDir = dir;
                    layer.AnimationFrame = 0;
                    (layer.Texture, layer.AnimationTimeLeft) = state.GetFrame(dir, 0);
                    doRedraw = true;
                }
                Layers[i] = layer;
            }

            if (doRedraw)
            {
                Redraw();
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
        }
    }
}
