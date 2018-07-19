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
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using SS14.Shared.Prototypes;
using SS14.Shared.Serialization;
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
                _visible = value;
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

        private DrawDepth drawDepth = DrawDepth.Objects;

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
                        Logger.ErrorS(LogCategory,
                                      "Layer '{0}'no longer has state '{1}' due to base RSI change. Trace:\n{2}",
                                      i, layer.State, Environment.StackTrace);
                        layer.Texture = null;
                    }
                }
            }
        }

        // TODO: Flyweighting per prototype with Copy-on-Write.
        private readonly Dictionary<object, int> LayerMap = new Dictionary<object, int>();

        // To a future Clusterfack:
        // REALLY BIG OPTIMIZATION POTENTIAL:
        // Layer is god damn huge. Copying it is expensive.
        // To be fair making it a class might be a good idea, making the following moot.
        // List<T> doesn't allow ref indexers because... reasons. Array does.
        // It may be a good idea to re-implement this list to use Layer[],
        // use ref locals EVERYWHERE, and handle the resizing ourselves.
        // This may be worth the overhead of basically reimplementing List<T>.
        private List<Layer> Layers;
        private readonly List<Godot.RID> CanvasItems = new List<Godot.RID>();

        private Godot.Node2D SceneNode;
        private IGodotTransformComponent TransformComponent;

        private IResourceCache resourceCache;
        private IPrototypeManager prototypes;

        public const string LogCategory = "go.comp.sprite";
        const string LayerSerializationCache = "spritelayer";

        /// <inheritdoc />
        public void LayerMapSet(object key, int layer)
        {
            if (layer < 0 || layer >= Layers.Count)
            {
                throw new ArgumentOutOfRangeException();
            }
            LayerMap.Add(key, layer);
        }

        /// <inheritdoc />
        public void LayerMapRemove(object key)
        {
            LayerMap.Remove(key);
        }

        /// <inheritdoc />
        public int LayerMapGet(object key)
        {
            return LayerMap[key];
        }

        /// <inheritdoc />
        public bool LayerMapTryGet(object key, out int layer)
        {
            return LayerMap.TryGetValue(key, out layer);
        }

        public int AddLayer(string texturePath, int? newIndex = null)
        {
            return AddLayer(new ResourcePath(texturePath), newIndex);
        }

        public int AddLayer(ResourcePath texturePath, int? newIndex = null)
        {
            if (!resourceCache.TryGetResource<TextureResource>(TextureRoot / texturePath, out var texture))
            {
                Logger.ErrorS(LogCategory, "Unable to load texture '{0}'. Trace:\n{1}", texturePath, Environment.StackTrace);
            }

            return AddLayer(texture, newIndex);
        }

        public int AddLayer(Texture texture, int? newIndex = null)
        {
            var layer = Layer.New();
            layer.Texture = texture;
            RedrawQueued = true;
            return AddLayer(layer, newIndex);
        }

        public int AddLayer(RSI.StateId stateId, int? newIndex = null)
        {
            var layer = Layer.New();
            layer.State = stateId;
            if (BaseRSI.TryGetState(stateId, out var state))
            {
                (layer.Texture, layer.AnimationTimeLeft) = state.GetFrame(layer.CurrentDir, 0);
            }
            else
            {
                Logger.ErrorS(LogCategory, "State does not exist in RSI: '{0}'. Trace:\n{1}", stateId, Environment.StackTrace);
            }
            RedrawQueued = true;
            return AddLayer(layer, newIndex);
        }

        public int AddLayerState(string stateId, int? newIndex = null)
        {
            return AddLayer(new RSI.StateId(stateId), newIndex);
        }

        public int AddLayer(RSI.StateId stateId, string rsiPath, int? newIndex = null)
        {
            return AddLayer(stateId, new ResourcePath(rsiPath), newIndex);
        }

        public int AddLayerState(string stateId, string rsiPath, int? newIndex = null)
        {
            return AddLayer(new RSI.StateId(stateId), rsiPath, newIndex);
        }

        public int AddLayer(RSI.StateId stateId, ResourcePath rsiPath, int? newIndex = null)
        {
            if (!resourceCache.TryGetResource<RSIResource>(TextureRoot / rsiPath, out var res))
            {
                Logger.ErrorS(LogCategory, "Unable to load RSI '{0}'. Trace:\n{1}", rsiPath, Environment.StackTrace);
            }

            return AddLayer(stateId, res?.RSI);
        }

        public int AddLayerState(string stateId, ResourcePath rsiPath, int? newIndex = null)
        {
            return AddLayer(new RSI.StateId(stateId), rsiPath, newIndex);
        }

        public int AddLayer(RSI.StateId stateId, RSI rsi, int? newIndex = null)
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
                Logger.ErrorS(LogCategory, "State does not exist in RSI: '{0}'. Trace:\n{1}", stateId, Environment.StackTrace);
            }
            RedrawQueued = true;
            return AddLayer(layer, newIndex);
        }

        public int AddLayerState(string stateId, RSI rsi, int? newIndex = null)
        {
            return AddLayer(new RSI.StateId(stateId), rsi, newIndex);
        }

        int AddLayer(Layer layer, int? newIndex)
        {
            if (newIndex.HasValue)
            {
                Layers.Insert(newIndex.Value, layer);
                foreach (var kv in LayerMap)
                {
                    if (kv.Value >= newIndex.Value)
                    {
                        LayerMap[kv.Key] = kv.Value + 1;
                    }
                }
                return newIndex.Value;
            }
            else
            {
                Layers.Add(layer);
                return Layers.Count - 1;
            }
        }

        public void RemoveLayer(int layer)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot remove! Trace:\n{1}", layer, Environment.StackTrace);
                return;
            }
            Layers.RemoveAt(layer);
            foreach (var kv in LayerMap)
            {
                if (kv.Value == layer)
                {
                    LayerMap.Remove(kv.Key);
                }

                else if (kv.Value > layer)
                {
                    LayerMap[kv.Key] = kv.Value - 1;
                }
            }
            RedrawQueued = true;
        }

        public void RemoveLayer(object layerKey)
        {
            if (!LayerMapTryGet(layerKey, out var layer))
            {
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot remove! Trace:\n{1}", layerKey, Environment.StackTrace);
                return;
            }

            RemoveLayer(layer);
        }

        public void LayerSetShader(int layer, Shader shader)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set shader! Trace:\n{1}", layer, Environment.StackTrace);
                return;
            }
            var thelayer = Layers[layer];
            thelayer.Shader = shader;
            Layers[layer] = thelayer;
            RedrawQueued = true;
        }

        public void LayerSetShader(object layerKey, Shader shader)
        {
            if (!LayerMapTryGet(layerKey, out var layer))
            {
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set shader! Trace:\n{1}", layerKey, Environment.StackTrace);
                return;
            }

            LayerSetShader(layer, shader);
        }

        public void LayerSetShader(int layer, string shaderName)
        {
            if (!prototypes.TryIndex<ShaderPrototype>(shaderName, out var prototype))
            {
                Logger.ErrorS(LogCategory, "Shader prototype '{0}' does not exist. Trace:\n{1}", shaderName, Environment.StackTrace);
            }

            // This will set the shader to null if it does not exist.
            LayerSetShader(layer, prototype?.Instance());
        }

        public void LayerSetShader(object layerKey, string shaderName)
        {
            if (!LayerMapTryGet(layerKey, out var layer))
            {
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set shader! Trace:\n{1}", layerKey, Environment.StackTrace);
                return;
            }

            LayerSetShader(layer, shaderName);
        }

        public void LayerSetTexture(int layer, Texture texture)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set texture! Trace:\n{1}", layer, Environment.StackTrace);
                return;
            }
            var thelayer = Layers[layer];
            thelayer.State = null;
            thelayer.Texture = texture;
            Layers[layer] = thelayer;
            RedrawQueued = true;
        }

        public void LayerSetTexture(object layerKey, Texture texture)
        {
            if (!LayerMapTryGet(layerKey, out var layer))
            {
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set texture! Trace:\n{1}", layerKey, Environment.StackTrace);
                return;
            }

            LayerSetTexture(layer, texture);
        }

        public void LayerSetTexture(int layer, string texturePath)
        {
            LayerSetTexture(layer, new ResourcePath(texturePath));
        }

        public void LayerSetTexture(object layerKey, string texturePath)
        {
            LayerSetTexture(layerKey, new ResourcePath(texturePath));
        }

        public void LayerSetTexture(int layer, ResourcePath texturePath)
        {
            if (!resourceCache.TryGetResource<TextureResource>(TextureRoot / texturePath, out var texture))
            {
                Logger.ErrorS(LogCategory, "Unable to load texture '{0}'. Trace:\n{1}", texturePath, Environment.StackTrace);
            }

            LayerSetTexture(layer, texture);
        }

        public void LayerSetTexture(object layerKey, ResourcePath texturePath)
        {
            if (!LayerMapTryGet(layerKey, out var layer))
            {
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set texture! Trace:\n{1}", layerKey, Environment.StackTrace);
                return;
            }

            LayerSetTexture(layer, texturePath);
        }

        public void LayerSetState(int layer, RSI.StateId stateId)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set state! Trace:\n{1}", layer, Environment.StackTrace);
                return;
            }

            var thelayer = Layers[layer];
            if (thelayer.State == stateId)
            {
                return;
            }
            thelayer.State = stateId;
            var rsi = thelayer.RSI ?? BaseRSI;
            if (rsi == null)
            {
                Logger.ErrorS(LogCategory, "No RSI to pull new state from! Trace:\n{0}", Environment.StackTrace);
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
                    Logger.ErrorS(LogCategory, "State '{0}' does not exist in RSI. Trace:\n{1}", stateId, Environment.StackTrace);
                    thelayer.Texture = null;
                }
            }

            Layers[layer] = thelayer;
            RedrawQueued = true;
        }

        public void LayerSetState(object layerKey, RSI.StateId stateId)
        {
            if (!LayerMapTryGet(layerKey, out var layer))
            {
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set state! Trace:\n{1}", layerKey, Environment.StackTrace);
                return;
            }

            LayerSetState(layer, stateId);
        }

        public void LayerSetState(int layer, RSI.StateId stateId, RSI rsi)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set state! Trace:\n{1}", layer, Environment.StackTrace);
                return;
            }

            var thelayer = Layers[layer];
            thelayer.State = stateId;
            thelayer.RSI = rsi;
            var actualrsi = thelayer.RSI ?? BaseRSI;
            if (actualrsi == null)
            {
                Logger.ErrorS(LogCategory, "No RSI to pull new state from! Trace:\n{0}", Environment.StackTrace);
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
                    Logger.ErrorS(LogCategory, "State '{0}' does not exist in RSI. Trace:\n{1}", stateId, Environment.StackTrace);
                    thelayer.Texture = null;
                }
            }

            Layers[layer] = thelayer;
            RedrawQueued = true;
        }

        public void LayerSetState(object layerKey, RSI.StateId stateId, RSI rsi)
        {
            if (!LayerMapTryGet(layerKey, out var layer))
            {
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set state! Trace:\n{1}", layerKey, Environment.StackTrace);
                return;
            }

            LayerSetState(layer, stateId, rsi);
        }

        public void LayerSetState(int layer, RSI.StateId stateId, string rsiPath)
        {
            LayerSetState(layer, stateId, new ResourcePath(rsiPath));
        }

        public void LayerSetState(object layerKey, RSI.StateId stateId, string rsiPath)
        {
            LayerSetState(layerKey, stateId, new ResourcePath(rsiPath));
        }

        public void LayerSetState(int layer, RSI.StateId stateId, ResourcePath rsiPath)
        {
            if (!resourceCache.TryGetResource<RSIResource>(TextureRoot / rsiPath, out var res))
            {
                Logger.ErrorS(LogCategory, "Unable to load RSI '{0}'. Trace:\n{1}", rsiPath, Environment.StackTrace);
            }

            LayerSetState(layer, stateId, res?.RSI);
        }

        public void LayerSetState(object layerKey, RSI.StateId stateId, ResourcePath rsiPath)
        {
            if (!LayerMapTryGet(layerKey, out var layer))
            {
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set state! Trace:\n{1}", layerKey, Environment.StackTrace);
                return;
            }

            LayerSetState(layer, stateId, rsiPath);
        }

        public void LayerSetRSI(int layer, RSI rsi)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set RSI! Trace:\n{1}", layer, Environment.StackTrace);
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
                Logger.ErrorS(LogCategory, "No RSI to pull new state from! Trace:\n{0}", Environment.StackTrace);
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
                    Logger.ErrorS(LogCategory, "State '{0}' does not exist in set RSI. Trace:\n{1}", thelayer.State, Environment.StackTrace);
                    thelayer.Texture = null;
                }
            }

            Layers[layer] = thelayer;
            RedrawQueued = true;
        }

        public void LayerSetRSI(object layerKey, RSI rsi)
        {
            if (!LayerMapTryGet(layerKey, out var layer))
            {
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set RSI! Trace:\n{1}", layerKey, Environment.StackTrace);
                return;
            }

            LayerSetRSI(layer, rsi);
        }

        public void LayerSetRSI(int layer, string rsiPath)
        {
            LayerSetRSI(layer, new ResourcePath(rsiPath));
        }

        public void LayerSetRSI(object layerKey, string rsiPath)
        {
            LayerSetRSI(layerKey, new ResourcePath(rsiPath));
        }

        public void LayerSetRSI(int layer, ResourcePath rsiPath)
        {
            if (!resourceCache.TryGetResource<RSIResource>(TextureRoot / rsiPath, out var res))
            {
                Logger.ErrorS(LogCategory, "Unable to load RSI '{0}'. Trace:\n{1}", rsiPath, Environment.StackTrace);
            }

            LayerSetRSI(layer, res?.RSI);
        }

        public void LayerSetRSI(object layerKey, ResourcePath rsiPath)
        {
            if (!LayerMapTryGet(layerKey, out var layer))
            {
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set RSI! Trace:\n{1}", layerKey, Environment.StackTrace);
                return;
            }

            LayerSetRSI(layer, rsiPath);
        }

        public void LayerSetScale(int layer, Vector2 scale)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set scale! Trace:\n{1}", layer, Environment.StackTrace);
                return;
            }

            var thelayer = Layers[layer];
            thelayer.Scale = scale;
            Layers[layer] = thelayer;
            RedrawQueued = true;
        }

        public void LayerSetScale(object layerKey, Vector2 scale)
        {
            if (!LayerMapTryGet(layerKey, out var layer))
            {
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set scale! Trace:\n{1}", layerKey, Environment.StackTrace);
                return;
            }

            LayerSetScale(layer, scale);
        }


        public void LayerSetRotation(int layer, Angle rotation)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set rotation! Trace:\n{1}", layer, Environment.StackTrace);
                return;
            }

            var thelayer = Layers[layer];
            thelayer.Rotation = rotation;
            Layers[layer] = thelayer;
            RedrawQueued = true;
        }

        public void LayerSetRotation(object layerKey, Angle rotation)
        {
            if (!LayerMapTryGet(layerKey, out var layer))
            {
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set rotation! Trace:\n{1}", layerKey, Environment.StackTrace);
                return;
            }

            LayerSetRotation(layer, rotation);
        }

        public void LayerSetVisible(int layer, bool visible)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set visibility! Trace:\n{1}", layer, Environment.StackTrace);
                return;
            }

            var thelayer = Layers[layer];
            thelayer.Visible = visible;
            Layers[layer] = thelayer;
            RedrawQueued = true;
        }

        public void LayerSetVisible(object layerKey, bool visible)
        {
            if (!LayerMapTryGet(layerKey, out var layer))
            {
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set visibility! Trace:\n{1}", layerKey, Environment.StackTrace);
                return;
            }

            LayerSetVisible(layer, visible);
        }

        public void LayerSetColor(int layer, Color color)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set color! Trace:\n{1}", layer, Environment.StackTrace);
                return;
            }

            var thelayer = Layers[layer];
            thelayer.Color = color;
            Layers[layer] = thelayer;
            RedrawQueued = true;
        }

        public void LayerSetColor(object layerKey, Color color)
        {
            if (!LayerMapTryGet(layerKey, out var layer))
            {
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set color! Trace:\n{1}", layerKey, Environment.StackTrace);
                return;
            }

            LayerSetColor(layer, color);
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
            RedrawQueued = true;
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
                if (!layer.Visible)
                {
                    continue;
                }
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
                texture.GodotTexture.Draw(currentItem, -texture.GodotTexture.GetSize() / 2, layer.Color.Convert());
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataFieldCached(ref scale, "scale", Vector2.One);
            serializer.DataFieldCached(ref rotation, "rotation", Angle.Zero);
            serializer.DataFieldCached(ref offset, "offset", Vector2.One);
            serializer.DataFieldCached(ref drawDepth, "drawdepth", DrawDepth.Objects);
            serializer.DataFieldCached(ref color, "color", Color.White);
            serializer.DataFieldCached(ref _directional, "directional", true);
            serializer.DataFieldCached(ref _visible, "visible", true);

            // TODO: Writing?
            if (!serializer.Reading)
            {
                return;
            }

            if (serializer.TryGetCacheData<List<Layer>>(LayerSerializationCache, out var layers))
            {
                Layers = layers.ShallowClone();
                return;
            }

            prototypes = IoCManager.Resolve<IPrototypeManager>();
            resourceCache = IoCManager.Resolve<IResourceCache>();

            var rsi = serializer.ReadDataField<string>("sprite", null);
            if (!string.IsNullOrWhiteSpace(rsi))
            {
                var rsiPath = TextureRoot / rsi;
                try
                {
                    BaseRSI = resourceCache.GetResource<RSIResource>(rsiPath).RSI;
                }
                catch
                {
                    Logger.ErrorS(LogCategory, "Unable to load RSI '{0}'.", rsiPath);
                }
            }

            layers = new List<Layer>();

            var layerData = serializer.ReadDataField<List<PrototypeLayerData>>("layers", new List<PrototypeLayerData>());

            var baseState = serializer.ReadDataField<string>("state", null);
            var texturePath = serializer.ReadDataField<string>("texture", null);

            layerData.Insert(0, new PrototypeLayerData
            {
                TexturePath = string.IsNullOrWhiteSpace(texturePath) ? null : texturePath,
                State = baseState,
                Color = Color.White,
                Scale = Vector2.One,
                Visible = true,
            });

            foreach (var layerDatum in layerData)
            {
                var layer = Layer.New();
                if (!string.IsNullOrWhiteSpace(layerDatum.RsiPath))
                {
                    var path = TextureRoot / layerDatum.RsiPath;
                    try
                    {
                        layer.RSI = resourceCache.GetResource<RSIResource>(path).RSI;
                    }
                    catch
                    {
                        Logger.ErrorS(LogCategory, "Unable to load layer RSI '{0}'.", path);
                    }
                }

                if (!string.IsNullOrWhiteSpace(layerDatum.State))
                {
                    var theRsi = layer.RSI ?? BaseRSI;
                    if (rsi == null)
                    {
                        Logger.ErrorS(LogCategory,
                                      "Layer has no RSI to load states from."
                                      + "cannot use 'state' property. Prototype: '{0}'", Owner.Prototype.ID);
                    }
                    else
                    {
                        var stateid = new RSI.StateId(layerDatum.State, RSI.Selectors.None);
                        layer.State = stateid;
                        if (BaseRSI.TryGetState(stateid, out var state))
                        {
                            layer.Texture = state.GetFrame(layer.CurrentDir, 0).icon;
                        }
                        else
                        {
                            Logger.ErrorS(LogCategory,
                                          "State not found in layer RSI: '{0}'.",
                                          stateid);
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(layerDatum.TexturePath))
                {
                    if (layer.State.IsValid)
                    {
                        Logger.ErrorS(LogCategory,
                                      "Cannot specify 'texture' on a layer if it has an RSI state specified."
                                     );
                    }
                    else
                    {
                        layer.Texture = resourceCache.GetResource<TextureResource>(TextureRoot / layerDatum.TexturePath);
                    }
                }

                if (!string.IsNullOrWhiteSpace(layerDatum.Shader))
                {
                    if (prototypes.TryIndex<ShaderPrototype>(layerDatum.Shader, out var prototype))
                    {
                        layer.Shader = prototype.Instance();
                    }
                    else
                    {
                        Logger.ErrorS(LogCategory,
                                      "Shader prototype '{0}' does not exist. Prototype: '{1}'",
                                      layerDatum.Shader, Owner.Prototype.ID);
                    }
                }

                layer.Color = layerDatum.Color;
                layer.Rotation = layerDatum.Rotation;
                layer.Visible = layerDatum.Visible;
                layer.Scale = layerDatum.Scale;

                layers.Add(layer);
            }

            Layers = layers;
            serializer.SetCacheData(LayerSerializationCache, Layers);
        }

        /*
                public override void LoadParameters(YamlMappingNode mapping)
                {
                    base.LoadParameters(mapping);

                    prototypes = IoCManager.Resolve<IPrototypeManager>();
                    resourceCache = IoCManager.Resolve<IResourceCache>();

                    if (mapping.TryGetNode("sprite", out var node))
                    {
                        var path = TextureRoot / node.AsResourcePath();
                        try
                        {
                            BaseRSI = resourceCache.GetResource<RSIResource>(path).RSI;
                        }
                        catch
                        {
                            Logger.ErrorS(LogCategory, "Unable to load RSI '{0}'. Prototype: '{1}'", path, Owner.Prototype.ID);
                        }
                    }

                    if (mapping.TryGetNode("state", out node))
                    {
                        if (BaseRSI == null)
                        {
                            Logger.ErrorS(LogCategory,
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
                                Logger.ErrorS(LogCategory,
                                              "State not found in RSI: '{0}'. Prototype: '{1}'",
                                              stateid, Owner.Prototype.ID);
                            }

                            Layers.Add(layer);
                        }
                    }

                    if (mapping.TryGetNode("texture", out node))
                    {
                        // Pretty much to allow people to override things defining texture in child prototypes.
                        if (!String.IsNullOrWhiteSpace(node.AsString()))
                        {
                            if (mapping.HasNode("state"))
                            {
                                Logger.ErrorS(LogCategory,
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
                            Logger.ErrorS(LogCategory, "Unable to load layer RSI '{0}'. Prototype: '{1}'", path, Owner.Prototype.ID);
                        }
                    }

                    if (mapping.TryGetNode("state", out node))
                    {
                        var rsi = layer.RSI ?? BaseRSI;
                        if (rsi == null)
                        {
                            Logger.ErrorS(LogCategory,
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
                                Logger.ErrorS(LogCategory,
                                              "State not found in layer RSI: '{0}'. Prototype: '{1}'",
                                              stateid, Owner.Prototype.ID);
                            }
                        }
                    }

                    if (mapping.TryGetNode("texture", out node))
                    {
                        if (layer.State.IsValid)
                        {
                            Logger.ErrorS(LogCategory,
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
                            Logger.ErrorS(LogCategory,
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

                    if (mapping.TryGetNode("visible", out node))
                    {
                        layer.Visible = node.AsBool();
                    }

                    if (mapping.TryGetNode("color", out node))
                    {
                        layer.Color = node.AsColor();
                    }

                    Layers.Add(layer);
                }
        */
        public void FrameUpdate(float delta)
        {
            if (Directional)
            {
                SceneNode.Rotation = (float)(-TransformComponent.WorldRotation + Rotation) + MathHelper.PiOver2;
            }

            for (var i = 0; i < Layers.Count; i++)
            {
                var layer = Layers[i];
                // Since State is a struct, we can't null-check it directly.
                if (!layer.State.IsValid || !layer.Visible)
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

        public override void HandleComponentState(ComponentState state)
        {
            var thestate = (SpriteComponentState)state;

            Visible = thestate.Visible;
            DrawDepth = thestate.DrawDepth;
            Scale = thestate.Scale;
            Rotation = thestate.Rotation;
            Offset = thestate.Offset;
            Color = thestate.Color;
            Directional = thestate.Directional;

            if (thestate.BaseRsiPath != null && BaseRSI != null)
            {
                if (resourceCache.TryGetResource<RSIResource>(TextureRoot / thestate.BaseRsiPath, out var res))
                {
                    if (BaseRSI != res.RSI)
                    {
                        BaseRSI = res.RSI;
                    }
                }
                else
                {
                    Logger.ErrorS(LogCategory, "Hey server, RSI '{0}' doesn't exist.", thestate.BaseRsiPath);
                }
            }

            // Maybe optimize this to NOT full clear.
            // At least we're not doing extra allocations,
            // because the list doesn't reallocate.
            Layers.Clear();
            for (var i = 0; i < thestate.Layers.Count; i++)
            {
                var netlayer = thestate.Layers[i];
                var layer = Layer.New();
                // These are easy so do them here.
                layer.Scale = netlayer.Scale;
                layer.Rotation = netlayer.Rotation;
                layer.Visible = netlayer.Visible;
                layer.Color = netlayer.Color;
                Layers.Add(layer);

                // Using the public API to handle errors.
                // Probably slow as crap.
                // Who am I kidding, DEFINITELY.
                if (netlayer.Shader != null)
                {
                    LayerSetShader(i, netlayer.Shader);
                }

                if (netlayer.RsiPath != null)
                {
                    LayerSetRSI(i, netlayer.RsiPath);
                }

                if (netlayer.TexturePath != null)
                {
                    LayerSetTexture(i, netlayer.TexturePath);
                }
                else if (netlayer.State != null)
                {
                    LayerSetState(i, netlayer.State);
                }
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
            public bool Visible;
            public Color Color;

            public static Layer New()
            {
                return new Layer()
                {
                    CurrentDir = RSI.State.Direction.South,
                    Scale = Vector2.One,
                    Visible = true,
                    Color = Color.White
                };
            }
        }

        private struct PrototypeLayerData : IExposeData
        {
            public string Shader;
            public string TexturePath;
            public string RsiPath;
            public string State;
            public Vector2 Scale;
            public Angle Rotation;
            public bool Visible;
            public Color Color;

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
            }
        }
    }
}
