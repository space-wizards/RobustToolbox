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
using SS14.Shared.GameObjects.Components.Renderable;
using SS14.Shared.Interfaces.GameObjects;
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
using System.Text;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;
using VS = Godot.VisualServer;

namespace SS14.Client.GameObjects
{
    public sealed class SpriteComponent : SharedSpriteComponent, ISpriteComponent, IClickTargetComponent,
        IComponentDebug
    {
        private bool _visible = true;

        [ViewVariables(VVAccess.ReadWrite)]
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
        [ViewVariables]
        public DrawDepth DrawDepth
        {
            get => drawDepth;
            set
            {
                drawDepth = value;

                if (GameController.OnGodot && SceneNode != null)
                {
                    SceneNode.ZIndex = (int) value;
                }
            }
        }

        private Vector2 scale = Vector2.One;

        /// <summary>
        ///     A scale applied to all layers.
        /// </summary>
        [ViewVariables]
        public Vector2 Scale
        {
            get => scale;
            set
            {
                scale = value;
                if (GameController.OnGodot && SceneNode != null)
                {
                    SceneNode.Scale = value.Convert();
                }
            }
        }

        private Angle rotation;

        [ViewVariables]
        public Angle Rotation
        {
            get => rotation;
            set
            {
                rotation = value;
                if (GameController.OnGodot && SceneNode != null)
                {
                    SceneNode.Rotation = (float) value;
                }
            }
        }

        private Vector2 offset = Vector2.Zero;

        /// <summary>
        ///     Offset applied to all layers.
        /// </summary>
        [ViewVariables]
        public Vector2 Offset
        {
            get => offset;
            set
            {
                offset = value;
                if (GameController.OnGodot && SceneNode != null)
                {
                    SceneNode.Position = value.Convert() * EyeManager.PIXELSPERMETER;
                }
            }
        }

        private Color color = Color.White;

        [ViewVariables]
        public Color Color
        {
            get => color;
            set
            {
                color = value;
                if (GameController.OnGodot && SceneNode != null)
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
        [ViewVariables]
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

        [ViewVariables(VVAccess.ReadWrite)] private bool RedrawQueued = true;

        private RSI _baseRsi;

        [ViewVariables]
        public RSI BaseRSI
        {
            get => _baseRsi;
            set
            {
                _baseRsi = value;
                if (Layers == null)
                {
                    return;
                }

                for (var i = 0; i < Layers.Count; i++)
                {
                    var layer = Layers[i];
                    if (!layer.State.IsValid || layer.RSI != null)
                    {
                        continue;
                    }

                    if (value.TryGetState(layer.State, out var state))
                    {
                        (layer.Texture, layer.AnimationTimeLeft) = state.GetFrame(CorrectLayerDir(ref layer, state), 0);
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

        [ViewVariables] private Dictionary<object, int> LayerMap = new Dictionary<object, int>();
        [ViewVariables] private bool _layerMapShared;

        // To a future Clusterfack:
        // REALLY BIG OPTIMIZATION POTENTIAL:
        // Layer is god damn huge. Copying it is expensive.
        // To be fair making it a class might be a good idea, making the following moot.
        // List<T> doesn't allow ref indexers because... reasons. Array does.
        // It may be a good idea to re-implement this list to use Layer[],
        // use ref locals EVERYWHERE, and handle the resizing ourselves.
        // This may be worth the overhead of basically reimplementing List<T>.
        [ViewVariables] private List<Layer> Layers;

        private Godot.Node2D SceneNode;

        private IResourceCache resourceCache;
        private IPrototypeManager prototypes;

        [ViewVariables(VVAccess.ReadWrite)] RSI.State.Direction LastDir;
        [ViewVariables(VVAccess.ReadWrite)] private bool _recalcDirections = false;

        int NextMirrorKey;

        // Do not directly store mirror instances, so that they can be picked up by the GC is not disposed correctly.
        // Don't need em anyways.
        readonly Dictionary<int, MirrorData> Mirrors = new Dictionary<int, MirrorData>();
        ISpriteProxy MainMirror;

        private static Shader _defaultShader;

        [ViewVariables]
        private static Shader DefaultShader => _defaultShader ??
                                               (_defaultShader = IoCManager.Resolve<IPrototypeManager>()
                                                   .Index<ShaderPrototype>("shaded")
                                                   .Instance());

        public const string LogCategory = "go.comp.sprite";
        const string LayerSerializationCache = "spritelayer";
        const string LayerMapSerializationCache = "spritelayermap";

        /// <inheritdoc />
        public void LayerMapSet(object key, int layer)
        {
            if (layer < 0 || layer >= Layers.Count)
            {
                throw new ArgumentOutOfRangeException();
            }

            _layerMapEnsurePrivate();
            LayerMap.Add(key, layer);
        }

        /// <inheritdoc />
        public void LayerMapRemove(object key)
        {
            _layerMapEnsurePrivate();
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

        private void _layerMapEnsurePrivate()
        {
            if (!_layerMapShared)
            {
                return;
            }

            LayerMap = LayerMap.ShallowClone();
            _layerMapShared = false;
        }

        public void LayerMapReserveBlank(object key)
        {
            if (LayerMapTryGet(key, out var _))
            {
                return;
            }

            LayerMapSet(key, AddBlankLayer());
        }

        public int AddBlankLayer(int? newIndex = null)
        {
            var layer = Layer.New();
            layer.Visible = false;
            // Redraw is probably not needed but eh?
            RedrawQueued = true;
            return AddLayer(layer, newIndex);
        }

        public int AddLayer(string texturePath, int? newIndex = null)
        {
            return AddLayer(new ResourcePath(texturePath), newIndex);
        }

        public int AddLayer(ResourcePath texturePath, int? newIndex = null)
        {
            if (!resourceCache.TryGetResource<TextureResource>(TextureRoot / texturePath, out var texture))
            {
                Logger.ErrorS(LogCategory, "Unable to load texture '{0}'. Trace:\n{1}", texturePath,
                    Environment.StackTrace);
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
                (layer.Texture, layer.AnimationTimeLeft) = state.GetFrame(CorrectLayerDir(ref layer, state), 0);
            }
            else
            {
                Logger.ErrorS(LogCategory, "State does not exist in RSI: '{0}'. Trace:\n{1}", stateId,
                    Environment.StackTrace);
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
                (layer.Texture, layer.AnimationTimeLeft) = state.GetFrame(CorrectLayerDir(ref layer, state), 0);
            }
            else
            {
                Logger.ErrorS(LogCategory, "State does not exist in RSI: '{0}'. Trace:\n{1}", stateId,
                    Environment.StackTrace);
            }

            RedrawQueued = true;
            return AddLayer(layer, newIndex);
        }

        public int AddLayerState(string stateId, RSI rsi, int? newIndex = null)
        {
            return AddLayer(new RSI.StateId(stateId), rsi, newIndex);
        }

        public int AddLayer(SpriteSpecifier specifier, int? newIndex = null)
        {
            switch (specifier)
            {
                case SpriteSpecifier.Texture tex:
                    return AddLayer(tex.TexturePath, newIndex);

                case SpriteSpecifier.Rsi rsi:
                    return AddLayerState(rsi.RsiState, rsi.RsiPath, newIndex);

                default:
                    throw new NotImplementedException();
            }
        }

        private int AddLayer(Layer layer, int? newIndex)
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
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot remove! Trace:\n{1}", layer,
                    Environment.StackTrace);
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
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot remove! Trace:\n{1}", layerKey,
                    Environment.StackTrace);
                return;
            }

            RemoveLayer(layer);
        }

        public void LayerSetShader(int layer, Shader shader)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set shader! Trace:\n{1}",
                    layer, Environment.StackTrace);
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
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set shader! Trace:\n{1}",
                    layerKey, Environment.StackTrace);
                return;
            }

            LayerSetShader(layer, shader);
        }

        public void LayerSetShader(int layer, string shaderName)
        {
            if (!prototypes.TryIndex<ShaderPrototype>(shaderName, out var prototype))
            {
                Logger.ErrorS(LogCategory, "Shader prototype '{0}' does not exist. Trace:\n{1}", shaderName,
                    Environment.StackTrace);
            }

            // This will set the shader to null if it does not exist.
            LayerSetShader(layer, prototype?.Instance());
        }

        public void LayerSetShader(object layerKey, string shaderName)
        {
            if (!LayerMapTryGet(layerKey, out var layer))
            {
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set shader! Trace:\n{1}",
                    layerKey, Environment.StackTrace);
                return;
            }

            LayerSetShader(layer, shaderName);
        }

        public void LayerSetSprite(int layer, SpriteSpecifier specifier)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set sprite! Trace:\n{1}",
                    layer, Environment.StackTrace);
                return;
            }

            switch (specifier)
            {
                case SpriteSpecifier.Texture tex:
                    LayerSetTexture(layer, tex.TexturePath);
                    break;
                case SpriteSpecifier.Rsi rsi:
                    LayerSetState(layer, rsi.RsiState, rsi.RsiPath);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public void LayerSetSprite(object layerKey, SpriteSpecifier specifier)
        {
            if (!LayerMapTryGet(layerKey, out var layer))
            {
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set sprite! Trace:\n{1}",
                    layerKey, Environment.StackTrace);
                return;
            }

            LayerSetSprite(layer, specifier);
        }

        public void LayerSetTexture(int layer, Texture texture)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set texture! Trace:\n{1}",
                    layer, Environment.StackTrace);
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
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set texture! Trace:\n{1}",
                    layerKey, Environment.StackTrace);
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
                Logger.ErrorS(LogCategory, "Unable to load texture '{0}'. Trace:\n{1}", texturePath,
                    Environment.StackTrace);
            }

            LayerSetTexture(layer, texture);
        }

        public void LayerSetTexture(object layerKey, ResourcePath texturePath)
        {
            if (!LayerMapTryGet(layerKey, out var layer))
            {
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set texture! Trace:\n{1}",
                    layerKey, Environment.StackTrace);
                return;
            }

            LayerSetTexture(layer, texturePath);
        }

        public void LayerSetState(int layer, RSI.StateId stateId)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set state! Trace:\n{1}",
                    layer, Environment.StackTrace);
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
                    thelayer.AnimationFrame = 0;
                    thelayer.AnimationTime = 0;
                    (thelayer.Texture, thelayer.AnimationTimeLeft) =
                        state.GetFrame(CorrectLayerDir(ref thelayer, state), 0);
                }
                else
                {
                    Logger.ErrorS(LogCategory, "State '{0}' does not exist in RSI. Trace:\n{1}", stateId,
                        Environment.StackTrace);
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
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set state! Trace:\n{1}",
                    layerKey, Environment.StackTrace);
                return;
            }

            LayerSetState(layer, stateId);
        }

        public void LayerSetState(int layer, RSI.StateId stateId, RSI rsi)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set state! Trace:\n{1}",
                    layer, Environment.StackTrace);
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
                    thelayer.AnimationFrame = 0;
                    thelayer.AnimationTime = 0;
                    (thelayer.Texture, thelayer.AnimationTimeLeft) =
                        state.GetFrame(CorrectLayerDir(ref thelayer, state), 0);
                }
                else
                {
                    Logger.ErrorS(LogCategory, "State '{0}' does not exist in RSI. Trace:\n{1}", stateId,
                        Environment.StackTrace);
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
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set state! Trace:\n{1}",
                    layerKey, Environment.StackTrace);
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
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set state! Trace:\n{1}",
                    layerKey, Environment.StackTrace);
                return;
            }

            LayerSetState(layer, stateId, rsiPath);
        }

        public void LayerSetRSI(int layer, RSI rsi)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set RSI! Trace:\n{1}", layer,
                    Environment.StackTrace);
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
                    (thelayer.Texture, thelayer.AnimationTimeLeft) =
                        state.GetFrame(CorrectLayerDir(ref thelayer, state), 0);
                }
                else
                {
                    Logger.ErrorS(LogCategory, "State '{0}' does not exist in set RSI. Trace:\n{1}", thelayer.State,
                        Environment.StackTrace);
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
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set RSI! Trace:\n{1}", layerKey,
                    Environment.StackTrace);
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
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set RSI! Trace:\n{1}", layerKey,
                    Environment.StackTrace);
                return;
            }

            LayerSetRSI(layer, rsiPath);
        }

        public void LayerSetScale(int layer, Vector2 scale)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set scale! Trace:\n{1}",
                    layer, Environment.StackTrace);
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
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set scale! Trace:\n{1}",
                    layerKey, Environment.StackTrace);
                return;
            }

            LayerSetScale(layer, scale);
        }


        public void LayerSetRotation(int layer, Angle rotation)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set rotation! Trace:\n{1}",
                    layer, Environment.StackTrace);
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
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set rotation! Trace:\n{1}",
                    layerKey, Environment.StackTrace);
                return;
            }

            LayerSetRotation(layer, rotation);
        }

        public void LayerSetVisible(int layer, bool visible)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set visibility! Trace:\n{1}",
                    layer, Environment.StackTrace);
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
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set visibility! Trace:\n{1}",
                    layerKey, Environment.StackTrace);
                return;
            }

            LayerSetVisible(layer, visible);
        }

        public void LayerSetColor(int layer, Color color)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set color! Trace:\n{1}",
                    layer, Environment.StackTrace);
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
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set color! Trace:\n{1}",
                    layerKey, Environment.StackTrace);
                return;
            }

            LayerSetColor(layer, color);
        }

        public void LayerSetDirOffset(int layer, DirectionOffset offset)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set dir offset! Trace:\n{1}",
                    layer, Environment.StackTrace);
                return;
            }

            var thelayer = Layers[layer];
            thelayer.DirOffset = offset;
            Layers[layer] = thelayer;
            _recalcDirections = true;
            // Do NOT queue redraw.
            // FrameUpdate handles it.
        }

        public void LayerSetDirOffset(object layerKey, DirectionOffset offset)
        {
            if (!LayerMapTryGet(layerKey, out var layer))
            {
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set dir offset! Trace:\n{1}",
                    layerKey, Environment.StackTrace);
                return;
            }

            LayerSetDirOffset(layer, offset);
        }

        public void LayerSetAnimationTime(int layer, float animationTime)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set animation time! Trace:\n{1}",
                    layer, Environment.StackTrace);
                return;
            }

            var theLayer = Layers[layer];
            if (theLayer.State == null)
            {
                return;
            }

            // TODO: This could throw exceptions which shouldn't happen I think.
            var state = (theLayer.RSI ?? BaseRSI)[theLayer.State];
            var correctDir = CorrectLayerDir(ref theLayer, state);
            if (animationTime > theLayer.AnimationTime)
            {
                // Handle advancing differently from going backwards.
                theLayer.AnimationTimeLeft -= (animationTime - theLayer.AnimationTime);
            }
            else
            {
                // Going backwards we re-calculate from zero.
                // Definitely possible to optimize this for going backwards but I'm too lazy to figure that out.
                theLayer.AnimationTimeLeft = -animationTime + state.GetFrame(correctDir, 0).delay;
                theLayer.AnimationFrame = 0;
            }

            theLayer.AnimationTime = animationTime;
            // After setting timing data correctly, run advance to get to the correct frame.
            _advanceFrameAnimation(ref theLayer, state, correctDir);
            // And set to said frame.
            theLayer.Texture = state.GetFrame(correctDir, theLayer.AnimationFrame).icon;
            Layers[layer] = theLayer;
            RedrawQueued = true;
        }

        public void LayerSetAnimationTime(object layerKey, float animationTime)
        {
            if (!LayerMapTryGet(layerKey, out var layer))
            {
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set animation time! Trace:\n{1}",
                    layerKey, Environment.StackTrace);
                return;
            }

            LayerSetAnimationTime(layer, animationTime);
        }

        public void LayerSetAutoAnimated(int layer, bool autoAnimated)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set auto animated! Trace:\n{1}",
                    layer, Environment.StackTrace);
                return;
            }

            var theLayer = Layers[layer];
            theLayer.AutoAnimated = autoAnimated;
            Layers[layer] = theLayer;
        }

        public void LayerSetAutoAnimated(object layerKey, bool autoAnimated)
        {
            if (!LayerMapTryGet(layerKey, out var layer))
            {
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set auto animated! Trace:\n{1}",
                    layerKey, Environment.StackTrace);
                return;
            }

            LayerSetAutoAnimated(layer, autoAnimated);
        }

        /// <inheritdoc />
        public RSI.StateId LayerGetState(int layer)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot get state! Trace:\n{1}",
                    layer, Environment.StackTrace);
                return null;
            }

            var thelayer = Layers[layer];
            return thelayer.State;
        }

        public RSI LayerGetActualRSI(int layer)
        {
            if (Layers.Count <= layer)
            {
                throw new ArgumentOutOfRangeException(nameof(layer), $"Layer '{layer}' does not exist.");
            }

            var theLayer = Layers[layer];
            return BaseRSI ?? theLayer.RSI;
        }

        public RSI LayerGetActualRSI(object layerKey)
        {
            if (!LayerMapTryGet(layerKey, out var layer))
            {
                throw new KeyNotFoundException($"Layer '{layerKey}' does not exist.");
            }

            return LayerGetActualRSI(layer);
        }

        public ISpriteProxy CreateProxy()
        {
            if (GameController.OnGodot)
            {
                var item = VS.CanvasItemCreate();
                RedrawQueued = true;
                return CreateMirror(item);
            }

            var key = NextMirrorKey++;
            var mirror = new SpriteMirror(key, this);
            Mirrors.Add(key, new MirrorData());
            return mirror;
        }

        ISpriteProxy CreateMirror(Godot.RID item)
        {
            var key = NextMirrorKey++;
            var mirror = new SpriteMirror(key, this, item);
            var data = new MirrorData
            {
                Root = item,
                Children = new List<Godot.RID>(),
                Visible = true,
            };
            Mirrors.Add(key, data);
            return mirror;
        }

        public override void OnAdd()
        {
            base.OnAdd();

            if (GameController.OnGodot)
            {
                SceneNode = new Godot.Node2D()
                {
                    Name = "Sprite",
                    ZIndex = (int) drawDepth,
                    Scale = scale.Convert(),
                    Position = offset.Convert(),
                    Modulate = color.Convert(),
                    Rotation = (float) rotation,
                };
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();

            foreach (var key in Mirrors.Keys.ToList())
            {
                DisposeMirror(key);
            }

            if (GameController.OnGodot)
            {
                MainMirror.Dispose();
            }

            if (GameController.OnGodot)
            {
                SceneNode.QueueFree();
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            if (!GameController.OnGodot)
            {
                return;
            }

            MainMirror = CreateMirror(SceneNode.GetCanvasItem());
            var mir = Mirrors[0];
            mir.DontFree = true;
            Mirrors[0] = mir;
            ((IGodotTransformComponent) Owner.Transform).SceneNode.AddChild(SceneNode);
        }

        private void ClearDraw()
        {
            if (!GameController.OnGodot)
            {
                return;
            }

            foreach (var data in Mirrors.Values)
            {
                foreach (var item in data.Children)
                {
                    VS.FreeRid(item);
                }

                data.Children.Clear();
            }
        }

        private void Redraw()
        {
            ClearDraw();

            if (!GameController.OnGodot)
            {
                return;
            }

            foreach (var data in Mirrors.Values)
            {
                if (!data.Visible)
                {
                    continue;
                }

                foreach (var layer in Layers)
                {
                    if (!layer.Visible)
                    {
                        continue;
                    }

                    var shader = layer.Shader ?? DefaultShader;
                    var texture = layer.Texture ?? resourceCache.GetFallback<TextureResource>();

                    var currentItem = VS.CanvasItemCreate();
                    VS.CanvasItemSetParent(currentItem, data.Root);
                    data.Children.Add(currentItem);
                    VS.CanvasItemSetMaterial(currentItem, shader.GodotMaterial.GetRid());

                    var transform = Godot.Transform2D.Identity;
                    DrawingHandle.SetTransform2DRotationAndScale(ref transform, -layer.Rotation, layer.Scale);
                    VS.CanvasItemAddSetTransform(currentItem, transform);
                    // Not instantiating a DrawingHandle here because those are ref types,
                    // and I really don't want the extra allocation.
                    texture.GodotTexture.Draw(currentItem, -texture.GodotTexture.GetSize() / 2, layer.Color.Convert());
                }
            }
        }

        internal void OpenGLRender(DrawingHandleWorld drawingHandle, bool useWorldTransform=true)
        {
            Matrix3 transform;
            if (useWorldTransform)
            {
                Angle angle;
                if (Directional)
                {
                    angle = -Owner.Transform.WorldRotation;
                }
                else
                {
                    angle = -new Angle(MathHelper.PiOver2);
                }
                transform = Matrix3.CreateRotation(angle);
                var worldTransform = Owner.Transform.WorldMatrix;
                transform.Multiply(ref worldTransform);
            }
            else
            {
                transform = Matrix3.Identity;
            }
            drawingHandle.SetTransform(transform);
            foreach (var layer in Layers)
            {
                if (!layer.Visible)
                {
                    continue;
                }

                // TODO: Implement layer-specific rotation and scale.
                var texture = layer.Texture ?? resourceCache.GetFallback<TextureResource>();
                drawingHandle.DrawTexture(texture, -(Vector2)texture.Size/(2f*EyeManager.PIXELSPERMETER), color);
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataFieldCached(ref scale, "scale", Vector2.One);
            serializer.DataFieldCached(ref rotation, "rotation", Angle.Zero);
            serializer.DataFieldCached(ref offset, "offset", Vector2.Zero);
            serializer.DataFieldCached(ref drawDepth, "drawdepth", DrawDepth.Objects);
            serializer.DataFieldCached(ref color, "color", Color.White);
            serializer.DataFieldCached(ref _directional, "directional", true);
            serializer.DataFieldCached(ref _visible, "visible", true);

            prototypes = IoCManager.Resolve<IPrototypeManager>();
            resourceCache = IoCManager.Resolve<IResourceCache>();

            // TODO: Writing?
            if (!serializer.Reading)
            {
                return;
            }

            {
                var rsi = serializer.ReadDataField<string>("sprite", null);
                if (!string.IsNullOrWhiteSpace(rsi))
                {
                    var rsiPath = TextureRoot / rsi;
                    try
                    {
                        BaseRSI = resourceCache.GetResource<RSIResource>(rsiPath).RSI;
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorS(LogCategory, "Unable to load RSI '{0}'. Trace:\n{1}", rsiPath, e);
                    }
                }
            }

            if (serializer.TryGetCacheData<List<Layer>>(LayerSerializationCache, out var layers))
            {
                LayerMap = serializer.GetCacheData<Dictionary<object, int>>(LayerMapSerializationCache);
                _layerMapShared = true;
                Layers = layers.ShallowClone();
                // Do this because the directions in the cache may not be correct for us.
                _recalcDirections = true;
                return;
            }

            layers = new List<Layer>();

            var layerMap = new Dictionary<object, int>();

            var layerData =
                serializer.ReadDataField("layers", new List<PrototypeLayerData>());

            {
                var baseState = serializer.ReadDataField<string>("state", null);
                var texturePath = serializer.ReadDataField<string>("texture", null);

                if (baseState != null || texturePath != null)
                {
                    layerData.Insert(0, new PrototypeLayerData
                    {
                        TexturePath = string.IsNullOrWhiteSpace(texturePath) ? null : texturePath,
                        State = string.IsNullOrWhiteSpace(baseState) ? null : baseState,
                        Color = Color.White,
                        Scale = Vector2.One,
                        Visible = true,
                    });
                }
            }

            var reflectionManager = IoCManager.Resolve<IReflectionManager>();

            foreach (var layerDatum in layerData)
            {
                var anyTextureAttempted = false;
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
                    anyTextureAttempted = true;
                    var theRsi = layer.RSI ?? BaseRSI;
                    if (theRsi == null)
                    {
                        Logger.ErrorS(LogCategory,
                            "Layer has no RSI to load states from."
                            + "cannot use 'state' property. Prototype: '{0}'", Owner.Prototype.ID);
                    }
                    else
                    {
                        var stateid = new RSI.StateId(layerDatum.State, RSI.Selectors.None);
                        layer.State = stateid;
                        if (theRsi.TryGetState(stateid, out var state))
                        {
                            // Always use south because this layer will be cached in the serializer.
                            (layer.Texture, layer.AnimationTimeLeft) = state.GetFrame(RSI.State.Direction.South, 0);
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
                    anyTextureAttempted = true;
                    if (layer.State.IsValid)
                    {
                        Logger.ErrorS(LogCategory,
                            "Cannot specify 'texture' on a layer if it has an RSI state specified."
                        );
                    }
                    else
                    {
                        layer.Texture =
                            resourceCache.GetResource<TextureResource>(TextureRoot / layerDatum.TexturePath);
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
                // If neither state: nor texture: were provided we assume that they want a blank invisible layer.
                layer.Visible = anyTextureAttempted && layerDatum.Visible;
                layer.Scale = layerDatum.Scale;

                layers.Add(layer);

                if (layerDatum.MapKeys != null)
                {
                    var index = layers.Count - 1;
                    foreach (var keyString in layerDatum.MapKeys)
                    {
                        object key;
                        if (reflectionManager.TryParseEnumReference(keyString, out var @enum))
                        {
                            key = @enum;
                        }
                        else
                        {
                            key = keyString;
                        }

                        if (layerMap.ContainsKey(key))
                        {
                            Logger.ErrorS(LogCategory, "Duplicate layer map key definition: {0}", key);
                            continue;
                        }

                        layerMap.Add(key, index);
                    }
                }
            }

            Layers = layers;
            LayerMap = layerMap;
            _layerMapShared = true;
            serializer.SetCacheData(LayerSerializationCache, Layers.ShallowClone());
            serializer.SetCacheData(LayerMapSerializationCache, layerMap);
            // Do this because the directions in the cache may not be correct.
            _recalcDirections = true;
        }

        public void FrameUpdate(float delta)
        {
            // TODO: This entire method is a hotspot of redundant code.
            // This is definitely gonna deserve some optimizations later down the line.

            // Counteract world rotation so this thing gets rendered straight.
            if (Directional && GameController.OnGodot)
            {
                SceneNode.Rotation = (float) (Owner.Transform.WorldRotation - Rotation) - MathHelper.PiOver2;
            }

            var dirWeAreFacing = GetDir();
            var dirChanged = false;

            if (LastDir != dirWeAreFacing || _recalcDirections)
            {
                dirChanged = true;
                LastDir = dirWeAreFacing;
                _recalcDirections = false;
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
                RSI.State.Direction layerSpecificDir;
                if (state.Directions == RSI.State.DirectionType.Dir1)
                {
                    layerSpecificDir = RSI.State.Direction.South;
                }
                else
                {
                    layerSpecificDir = OffsetRsiDir(dirWeAreFacing, layer.DirOffset);
                }

                layer.AnimationTime += delta;
                if (!dirChanged)
                {
                    var delayCount = state.DelayCount(layerSpecificDir);
                    if (delayCount < 2 || !layer.AutoAnimated)
                    {
                        // Don't bother animating this.
                        // There's no animation frames!
                        continue;
                    }

                    layer.AnimationTimeLeft -= delta;
                }
                else
                {
                    // Mess with animation data so _advanceFrameAnimation fixes the position to where it should be.
                    // So cross-direction animations are synced so long as they have the same total length.
                    layer.AnimationFrame = 0;
                    layer.AnimationTimeLeft = -layer.AnimationTime;
                }

                _advanceFrameAnimation(ref layer, state, layerSpecificDir);
                layer.Texture = state.GetFrame(layerSpecificDir, layer.AnimationFrame).icon;

                RedrawQueued = true;
                Layers[i] = layer;
            }

            if (GameController.OnGodot && RedrawQueued)
            {
                Redraw();
                RedrawQueued = false;
            }
        }

        private static void _advanceFrameAnimation(ref Layer layer, RSI.State state, RSI.State.Direction layerSpecificDir)
        {
            var delayCount = state.DelayCount(layerSpecificDir);
            while (layer.AnimationTimeLeft < 0)
            {
                if (++layer.AnimationFrame >= delayCount)
                {
                    layer.AnimationFrame = 0;
                    layer.AnimationTime = -layer.AnimationTimeLeft;
                }

                layer.AnimationTimeLeft += state.GetFrame(layerSpecificDir, layer.AnimationFrame).delay;
            }
        }

        public override void HandleComponentState(ComponentState state)
        {
            var thestate = (SpriteComponentState) state;

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
            if (!Directional)
            {
                return RSI.State.Direction.South;
            }

            var angle = new Angle(Owner.Transform.WorldRotation);
            return angle.GetDir().Convert();
        }

        private static RSI.State.Direction OffsetRsiDir(RSI.State.Direction dir, DirectionOffset offset)
        {
            // There is probably a better way to do this.
            // Eh.
            switch (offset)
            {
                case DirectionOffset.None:
                    return dir;
                case DirectionOffset.Clockwise:
                    switch (dir)
                    {
                        case RSI.State.Direction.North:
                            return RSI.State.Direction.East;
                        case RSI.State.Direction.East:
                            return RSI.State.Direction.South;
                        case RSI.State.Direction.South:
                            return RSI.State.Direction.West;
                        case RSI.State.Direction.West:
                            return RSI.State.Direction.North;
                        default:
                            throw new NotImplementedException();
                    }
                case DirectionOffset.CounterClockwise:
                    switch (dir)
                    {
                        case RSI.State.Direction.North:
                            return RSI.State.Direction.West;
                        case RSI.State.Direction.East:
                            return RSI.State.Direction.North;
                        case RSI.State.Direction.South:
                            return RSI.State.Direction.East;
                        case RSI.State.Direction.West:
                            return RSI.State.Direction.South;
                        default:
                            throw new NotImplementedException();
                    }
                case DirectionOffset.Flip:
                    switch (dir)
                    {
                        case RSI.State.Direction.North:
                            return RSI.State.Direction.South;
                        case RSI.State.Direction.East:
                            return RSI.State.Direction.West;
                        case RSI.State.Direction.South:
                            return RSI.State.Direction.North;
                        case RSI.State.Direction.West:
                            return RSI.State.Direction.East;
                        default:
                            throw new NotImplementedException();
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        public string GetDebugString()
        {
            var builder = new StringBuilder();
            builder.AppendFormat(
                "vis/depth/scl/rot/ofs/col/diral/dir: {0}/{1}/{2}/{3}/{4}/{5}/{6}/{7}\n",
                Visible, DrawDepth, Scale, Rotation, Offset,
                Color, Directional, GetDir()
            );

            foreach (var layer in Layers)
            {
                builder.AppendFormat(
                    "shad/tex/rsi/state/ant/anf/scl/rot/vis/col/dofs: {0}/{1}/{2}/{3}/{4}/{5}/{6}/{7}/{8}/{9}/{10}\n",
                    // These are references and don't include useful data for knowing where they came from, sadly.
                    // "is one set" is better than nothing at least.
                    layer.Shader != null, layer.Texture != null, layer.RSI != null,
                    layer.State,
                    layer.AnimationTimeLeft, layer.AnimationFrame, layer.Scale, layer.Rotation, layer.Visible,
                    layer.Color, layer.DirOffset
                );
            }

            return builder.ToString();
        }

        RSI.State.Direction CorrectLayerDir(ref Layer layer, RSI.State state)
        {
            if (state.Directions == RSI.State.DirectionType.Dir1)
            {
                return RSI.State.Direction.South;
            }

            return OffsetRsiDir(GetDir(), layer.DirOffset);
        }

        private void DisposeMirror(int key)
        {
            if (!Mirrors.TryGetValue(key, out var val))
            {
                // Maybe possible if the sprite gets disposed before the mirror handle?
                return;
            }

            // TODO: Doing a full redraw when a mirror is disposed is kinda a waste.
            ClearDraw();
            RedrawQueued = true;
            if (!val.DontFree && GameController.OnGodot)
            {
                VS.FreeRid(val.Root);
            }

            Mirrors.Remove(key);
        }

        private bool IsMirrorDisposed(int key)
        {
            return Mirrors.ContainsKey(key);
        }

        void MirrorSetVisible(int key, bool visible)
        {
            var mirror = Mirrors[key];
            mirror.Visible = visible;
            Mirrors[key] = mirror;
            RedrawQueued = true;
        }

        /// <summary>
        ///     Enum to "offset" a cardinal direction.
        /// </summary>
        public enum DirectionOffset
        {
            /// <summary>
            ///     No offset.
            /// </summary>
            None = 0,

            /// <summary>
            ///     Rotate direction clockwise. (North -> East, etc...)
            /// </summary>
            Clockwise = 1,

            /// <summary>
            ///     Rotate direction counter-clockwise. (North -> West, etc...)
            /// </summary>
            CounterClockwise = 2,

            /// <summary>
            ///     Rotate direction 180 degrees, so flip. (North -> South, etc...)
            /// </summary>
            Flip = 3,
        }

        private struct Layer
        {
            public Shader Shader;
            public Texture Texture;

            public RSI RSI;
            public RSI.StateId State;
            public float AnimationTimeLeft;
            public float AnimationTime;
            public int AnimationFrame;
            public Vector2 Scale;
            public Angle Rotation;
            public bool Visible;
            public Color Color;
            public DirectionOffset DirOffset;
            public bool AutoAnimated;

            public static Layer New()
            {
                return new Layer
                {
                    Scale = Vector2.One,
                    Visible = true,
                    Color = Color.White,
                    AutoAnimated = true,
                };
            }
        }

        sealed class SpriteMirror : ISpriteProxy
        {
            readonly int Key;
            readonly SpriteComponent Master;
            private Godot.RID CanvasItem;
            private Godot.RID Parent;
            private Vector2 _offset;

            public Vector2 Offset
            {
                get => _offset;
                set
                {
                    CheckDisposed();
                    _offset = value;
                    UpdateTransform();
                }
            }

            private bool _visible = true;

            public bool Visible
            {
                get => _visible;
                set
                {
                    _visible = value;
                    Master.MirrorSetVisible(Key, value);
                }
            }

            public SpriteMirror(int key, SpriteComponent master, Godot.RID canvasItem) : this(key, master)
            {
                CanvasItem = canvasItem;
            }

            public SpriteMirror(int key, SpriteComponent master)
            {
                Master = master;
                Key = key;
            }

            public bool Disposed { get; private set; }

            private void CheckDisposed()
            {
                if (Disposed)
                {
                    throw new ObjectDisposedException(nameof(SpriteMirror));
                }
            }

            private void UpdateTransform()
            {
                if (!GameController.OnGodot)
                {
                    return;
                }

                var transform = new Godot.Transform2D(0, Offset.Convert());
                VS.CanvasItemSetTransform(CanvasItem, transform);
            }

            public void AttachToItem(Godot.RID item)
            {
                CheckDisposed();
                Parent = item;
                VS.CanvasItemSetParent(CanvasItem, Parent);
            }

            public void Dispose()
            {
                if (Disposed)
                {
                    return;
                }

                Dispose(true);
                GC.SuppressFinalize(this);
            }

            ~SpriteMirror()
            {
                Dispose(false);
            }

            void Dispose(bool disposing)
            {
                Master.DisposeMirror(Key);

                if (GameController.OnGodot)
                {
                    CanvasItem = null;
                }

                Disposed = true;
            }
        }

        struct MirrorData
        {
            public Godot.RID Root;
            public List<Godot.RID> Children;

            public bool Visible;

            // Don't free the canvas item if it's the scene node item.
            // That causes problems.
            // Seriously.
            public bool DontFree;
        }
    }
}
