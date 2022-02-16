using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using DrawDepthTag = Robust.Shared.GameObjects.DrawDepth;
using RSIDirection = Robust.Client.Graphics.RSI.State.Direction;

namespace Robust.Client.GameObjects
{
    [ComponentReference(typeof(SharedSpriteComponent))]
    [ComponentReference(typeof(ISpriteComponent))]
    public sealed class SpriteComponent : SharedSpriteComponent, ISpriteComponent,
        IComponentDebug, ISerializationHooks
    {
        [Dependency] private readonly IResourceCache resourceCache = default!;
        [Dependency] private readonly IPrototypeManager prototypes = default!;
        [Dependency] private readonly IEntityManager entities = default!;
        [Dependency] private readonly IEyeManager eyeManager = default!;

        [DataField("visible")]
        private bool _visible = true;

        [ViewVariables(VVAccess.ReadWrite)]
        public override bool Visible
        {
            get => _visible;
            set
            {
                if (_visible == value) return;
                _visible = value;

                entities.EventBus.RaiseLocalEvent(Owner, new SpriteUpdateEvent());
            }
        }

        [DataField("drawdepth", customTypeSerializer: typeof(ConstantSerializer<DrawDepthTag>))]
        private int drawDepth = DrawDepthTag.Default;

        /// <summary>
        ///     Z-index for drawing.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int DrawDepth
        {
            get => drawDepth;
            set => drawDepth = value;
        }

        [DataField("scale")]
        private Vector2 scale = Vector2.One;

        /// <summary>
        ///     A scale applied to all layers.
        /// </summary>
        [Animatable]
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 Scale
        {
            get => scale;
            set 
            {
                scale = value;
                UpdateLocalMatrix();
            }
        }

        [DataField("rotation")]
        private Angle rotation = Angle.Zero;

        [Animatable]
        [ViewVariables(VVAccess.ReadWrite)]
        public Angle Rotation
        {
            get => rotation;
            set
            {
                rotation = value;
                UpdateLocalMatrix();
            }
        }

        [DataField("offset")]
        private Vector2 offset = Vector2.Zero;

        /// <summary>
        ///     Offset applied to all layers.
        /// </summary>
        [Animatable]
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 Offset
        {
            get => offset;
            set
            {
                offset = value;
                UpdateLocalMatrix();
            }
        }

        [DataField("color")]
        private Color color = Color.White;

        public Matrix3 LocalMatrix = Matrix3.Identity;

        [Animatable]
        [ViewVariables(VVAccess.ReadWrite)]
        public Color Color
        {
            get => color;
            set => color = value;
        }

        [ViewVariables]
        internal RenderingTreeComponent? RenderTree { get; set; } = null;

        [DataField("layerDatums")]
        private List<PrototypeLayerData> LayerDatums
        {
            get
            {
                var layerDatums = new List<PrototypeLayerData>();
                foreach (var layer in Layers)
                {
                    layerDatums.Add(layer.ToPrototypeData());
                }

                return layerDatums;
            }
            set
            {
                if (value == null) return;

                Layers.Clear();
                foreach (var layerDatum in value)
                {
                    var anyTextureAttempted = false;
                    var layer = new Layer(this);
                    if (!string.IsNullOrWhiteSpace(layerDatum.RsiPath))
                    {
                        var path = TextureRoot / layerDatum.RsiPath;

                        if (IoCManager.Resolve<IResourceCache>().TryGetResource(path, out RSIResource? resource))
                        {
                            layer.RSI = resource.RSI;
                        }
                        else
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
                                "Layer has no RSI to load states from. Cannot use 'state' property. ({0})",
                                layerDatum.State);
                        }
                        else
                        {
                            var stateid = new RSI.StateId(layerDatum.State);
                            layer.State = stateid;
                            if (theRsi.TryGetState(stateid, out var state))
                            {
                                // Always use south because this layer will be cached in the serializer.
                                layer.AnimationTimeLeft = state.GetDelay(0);
                            }
                            else
                            {
                                Logger.ErrorS(LogCategory,
                                    $"State '{stateid}' not found in RSI: '{theRsi.Path}'.",
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
                                IoCManager.Resolve<IResourceCache>().GetResource<TextureResource>(TextureRoot / layerDatum.TexturePath);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(layerDatum.Shader))
                    {
                        if (IoCManager.Resolve<IPrototypeManager>().TryIndex<ShaderPrototype>(layerDatum.Shader, out var prototype))
                        {
                            layer.Shader = prototype.Instance();
                        }
                        else
                        {
                            Logger.ErrorS(LogCategory,
                                "Shader prototype '{0}' does not exist.",
                                layerDatum.Shader);
                        }
                    }

                    layer.Color = layerDatum.Color;
                    layer._rotation = layerDatum.Rotation;
                    layer._offset = layerDatum.Offset;
                    layer._scale = layerDatum.Scale;
                    layer.UpdateLocalMatrix();
                    // If neither state: nor texture: were provided we assume that they want a blank invisible layer.
                    layer.Visible = anyTextureAttempted && layerDatum.Visible;

                    Layers.Add(layer);

                    if (layerDatum.MapKeys != null)
                    {
                        var index = Layers.Count - 1;
                        foreach (var keyString in layerDatum.MapKeys)
                        {
                            object key;
                            if (IoCManager.Resolve<IReflectionManager>().TryParseEnumReference(keyString, out var @enum))
                            {
                                key = @enum;
                            }
                            else
                            {
                                key = keyString;
                            }

                            if (LayerMap.ContainsKey(key))
                            {
                                Logger.ErrorS(LogCategory, "Duplicate layer map key definition: {0}", key);
                                continue;
                            }

                            LayerMap.Add(key, index);
                        }
                    }
                }

                _layerMapShared = true;
                QueueUpdateIsInert();
            }
        }

        private RSI? _baseRsi;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("rsi", priority: 2)]
        public RSI? BaseRSI
        {
            get => _baseRsi;
            set
            {
                _baseRsi = value;
                if (value == null)
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
                        layer.AnimationTimeLeft = state.GetDelay(0);
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

        [DataField("sprite", readOnly: true)] private string? rsi;
        [DataField("layers", readOnly: true)] private List<PrototypeLayerData> layerDatums = new();

        [DataField("state", readOnly: true)] private string? state;
        [DataField("texture", readOnly: true)] private string? texture;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ContainerOccluded
        {
            get => _containerOccluded;
            set
            {
                if (_containerOccluded == value) return;
                _containerOccluded = value;
                entities.EventBus.RaiseLocalEvent(Owner, new SpriteUpdateEvent());
            }
        }

        private bool _containerOccluded;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool TreeUpdateQueued { get; set; }

        [ViewVariables(VVAccess.ReadWrite)] private bool _inertUpdateQueued;

        [ViewVariables(VVAccess.ReadWrite)]
        public ShaderInstance? PostShader { get; set; }

        [ViewVariables] private Dictionary<object, int> LayerMap = new();
        [ViewVariables] private bool _layerMapShared;
        [ViewVariables] private List<Layer> Layers = new();

        [ViewVariables(VVAccess.ReadWrite)] public uint RenderOrder { get; set; }

        // TODO: this should absolutely not be static.
        private static ShaderInstance? _defaultShader;

        [ViewVariables]
        private ShaderInstance? DefaultShader => _defaultShader ??= prototypes
            .Index<ShaderPrototype>("shaded")
            .Instance();

        public const string LogCategory = "go.comp.sprite";
        const string LayerSerializationCache = "spritelayer";
        const string LayerMapSerializationCache = "spritelayermap";

        [ViewVariables(VVAccess.ReadWrite)] public bool IsInert { get; private set; }

        void ISerializationHooks.AfterDeserialization()
        {
            {
                if (!string.IsNullOrWhiteSpace(rsi))
                {
                    var rsiPath = TextureRoot / rsi;
                    if(IoCManager.Resolve<IResourceCache>().TryGetResource(rsiPath, out RSIResource? resource))
                    {
                        BaseRSI = resource.RSI;
                    }
                    else
                    {
                        Logger.ErrorS(LogCategory, "Unable to load RSI '{0}'.", rsiPath);
                    }
                }
            }

            if (layerDatums.Count == 0)
            {
                if (state != null || texture != null)
                {
                    layerDatums.Insert(0, new PrototypeLayerData
                    {
                        TexturePath = string.IsNullOrWhiteSpace(texture) ? null : texture,
                        State = string.IsNullOrWhiteSpace(state) ? null : state,
                        Color = Color.White,
                        Scale = Vector2.One,
                        Visible = true,
                    });
                    state = null;
                    texture = null;
                }
            }

            if (layerDatums.Count != 0)
            {
                LayerMap.Clear();
                LayerDatums = layerDatums;
            }

            UpdateLocalMatrix();
        }

        /// <summary>
        /// Update this sprite component to visibly match the current state of other at the time
        /// this is called. Does not keep them perpetually in sync.
        /// This does some deep copying thus exerts some gc pressure, so avoid this for hot code paths.
        /// </summary>
        public void CopyFrom(SpriteComponent other)
        {
            //deep copying things to avoid entanglement
            _baseRsi = other._baseRsi;
            _visible = other._visible;
            _layerMapShared = other._layerMapShared;
            color = other.color;
            offset = other.offset;
            rotation = other.rotation;
            scale = other.scale;
            UpdateLocalMatrix();
            drawDepth = other.drawDepth;
            _screenLock = other._screenLock;
            _overrideDirection = other._overrideDirection;
            _enableOverrideDirection = other._enableOverrideDirection;
            Layers = new List<Layer>(other.Layers.Count);
            foreach (var otherLayer in other.Layers)
            {
                Layers.Add(new Layer(otherLayer, this));
            }
            IsInert = other.IsInert;
            LayerMap = other.LayerMap.ToDictionary(entry => entry.Key,
                entry => entry.Value);
            if (other.PostShader != null)
            {
                // only need to copy the shader if it's mutable
                PostShader = other.PostShader.Mutable ? other.PostShader.Duplicate() : other.PostShader;
            }
            else
            {
                PostShader = null;
            }

            RenderOrder = other.RenderOrder;
        }

        internal void UpdateLocalMatrix()
        {
            LocalMatrix = Matrix3.CreateTransform(in offset, in rotation, in scale);
        }

        public Matrix3 GetLocalMatrix()
        {
            return LocalMatrix;
        }

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
            var layer = new Layer(this) { Visible = false };
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
                if (texturePath.Extension == "rsi")
                {
                    Logger.ErrorS(LogCategory,
                        "Expected texture but got rsi '{0}', did you mean 'sprite:' instead of 'texture:'?",
                        texturePath);
                }

                Logger.ErrorS(LogCategory, "Unable to load texture '{0}'. Trace:\n{1}", texturePath,
                    Environment.StackTrace);
            }

            return AddLayer(texture?.Texture, newIndex);
        }

        public int AddLayer(Texture? texture, int? newIndex = null)
        {
            var layer = new Layer(this) { Texture = texture };
            return AddLayer(layer, newIndex);
        }

        public int AddLayer(RSI.StateId stateId, int? newIndex = null)
        {
            var layer = new Layer(this) { State = stateId };
            if (BaseRSI != null && BaseRSI.TryGetState(stateId, out var state))
            {
                layer.AnimationTimeLeft = state.GetDelay(0);
            }
            else
            {
                Logger.ErrorS(LogCategory, "State does not exist in RSI: '{0}'. Trace:\n{1}", stateId,
                    Environment.StackTrace);
            }

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

            return AddLayer(stateId, res?.RSI, newIndex);
        }

        public int AddLayerState(string stateId, ResourcePath rsiPath, int? newIndex = null)
        {
            return AddLayer(new RSI.StateId(stateId), rsiPath, newIndex);
        }

        public int AddLayer(RSI.StateId stateId, RSI? rsi, int? newIndex = null)
        {
            var layer = new Layer(this) { State = stateId, RSI = rsi };
            if (rsi != null && rsi.TryGetState(stateId, out var state))
            {
                layer.AnimationTimeLeft = state.GetDelay(0);
            }
            else
            {
                Logger.ErrorS(LogCategory, "State does not exist in RSI: '{0}'. Trace:\n{1}", stateId,
                    Environment.StackTrace);
            }

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
            int index;
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

                index = newIndex.Value;
            }
            else
            {
                Layers.Add(layer);
                index = Layers.Count - 1;
            }

            QueueUpdateIsInert();
            return index;
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

            QueueUpdateIsInert();
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

        public void LayerSetShader(int layer, ShaderInstance? shader)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set shader! Trace:\n{1}",
                    layer, Environment.StackTrace);
                return;
            }

            var theLayer = Layers[layer];
            theLayer.Shader = shader;
        }

        public void LayerSetShader(object layerKey, ShaderInstance shader)
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

        public void LayerSetTexture(int layer, Texture? texture)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set texture! Trace:\n{1}",
                    layer, Environment.StackTrace);
                return;
            }

            var theLayer = Layers[layer];
            theLayer.SetTexture(texture);
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
                if (texturePath.Extension == "rsi")
                {
                    Logger.ErrorS(LogCategory,
                        "Expected texture but got rsi '{0}', did you mean 'sprite:' instead of 'texture:'?",
                        texturePath);
                }

                Logger.ErrorS(LogCategory, "Unable to load texture '{0}'. Trace:\n{1}", texturePath,
                    Environment.StackTrace);
            }

            LayerSetTexture(layer, texture?.Texture);
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

            var theLayer = Layers[layer];
            theLayer.SetState(stateId);
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

        public void LayerSetState(int layer, RSI.StateId stateId, RSI? rsi)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set state! Trace:\n{1}",
                    layer, Environment.StackTrace);
                return;
            }

            var theLayer = Layers[layer];
            theLayer.State = stateId;
            theLayer.RSI = rsi;
            var actualRsi = theLayer.RSI ?? BaseRSI;
            if (actualRsi == null)
            {
                Logger.ErrorS(LogCategory, "No RSI to pull new state from! Trace:\n{0}", Environment.StackTrace);
                theLayer.Texture = null;
            }
            else
            {
                if (actualRsi.TryGetState(stateId, out var state))
                {
                    theLayer.AnimationFrame = 0;
                    theLayer.AnimationTime = 0;
                    theLayer.AnimationTimeLeft = state.GetDelay(0);
                }
                else
                {
                    Logger.ErrorS(LogCategory, "State '{0}' does not exist in RSI {1}. Trace:\n{2}", stateId,
                        actualRsi.Path, Environment.StackTrace);
                    theLayer.Texture = null;
                }
            }
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

        public void LayerSetRSI(int layer, RSI? rsi)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory, "Layer with index '{0}' does not exist, cannot set RSI! Trace:\n{1}", layer,
                    Environment.StackTrace);
                return;
            }

            var theLayer = Layers[layer];
            theLayer.SetRsi(rsi);
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

            var theLayer = Layers[layer];
            theLayer.Scale = scale;
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

            var theLayer = Layers[layer];
            theLayer.Rotation = rotation;
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

            Layers[layer].SetVisible(visible);
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

            var theLayer = Layers[layer];
            theLayer.Color = color;
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

            var theLayer = Layers[layer];
            theLayer.DirOffset = offset;
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
                Logger.ErrorS(LogCategory,
                    "Layer with index '{0}' does not exist, cannot set animation time! Trace:\n{1}",
                    layer, Environment.StackTrace);
                return;
            }

            Layers[layer].SetAnimationTime(animationTime);
        }

        public void LayerSetAnimationTime(object layerKey, float animationTime)
        {
            if (!LayerMapTryGet(layerKey, out var layer))
            {
                Logger.ErrorS(LogCategory,
                    "Layer with key '{0}' does not exist, cannot set animation time! Trace:\n{1}",
                    layerKey, Environment.StackTrace);
                return;
            }

            LayerSetAnimationTime(layer, animationTime);
        }

        public void LayerSetAutoAnimated(int layer, bool autoAnimated)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory,
                    "Layer with index '{0}' does not exist, cannot set auto animated! Trace:\n{1}",
                    layer, Environment.StackTrace);
                return;
            }

            Layers[layer].SetAutoAnimated(autoAnimated);
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

        public void LayerSetOffset(int layer, Vector2 layerOffset)
        {
            if (Layers.Count <= layer)
            {
                Logger.ErrorS(LogCategory,
                    "Layer with index '{0}' does not exist, cannot set offset! Trace:\n{1}",
                    layer, Environment.StackTrace);
                return;
            }

            Layers[layer].Offset = layerOffset;
        }

        public void LayerSetOffset(object layerKey, Vector2 layerOffset)
        {
            if (!LayerMapTryGet(layerKey, out var layer))
            {
                Logger.ErrorS(LogCategory, "Layer with key '{0}' does not exist, cannot set offset! Trace:\n{1}",
                    layerKey, Environment.StackTrace);
                return;
            }

            LayerSetOffset(layer, layerOffset);
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

        public RSI? LayerGetActualRSI(int layer)
        {
            return this[layer].ActualRsi;
        }

        public RSI? LayerGetActualRSI(object layerKey)
        {
            return this[layerKey].ActualRsi;
        }

        public ISpriteLayer this[int layer] => Layers[layer];
        public ISpriteLayer this[Index layer] => Layers[layer];
        public ISpriteLayer this[object layerKey] => this[LayerMap[layerKey]];
        public IEnumerable<ISpriteLayer> AllLayers => Layers;

        // Lobby SpriteView rendering path
        internal void Render(DrawingHandleWorld drawingHandle, Angle eyeRotation, Angle worldRotation, Direction? overrideDirection = null)
        {
            RenderInternal(drawingHandle, eyeRotation, worldRotation, Vector2.Zero, overrideDirection);
        }

        [DataField("noRot")] private bool _screenLock = false;

        [DataField("overrideDir")]
        private Direction _overrideDirection = Direction.East;

        [DataField("enableOverrideDir")]
        private bool _enableOverrideDirection;

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public bool NoRotation { get => _screenLock; set => _screenLock = value; }

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public Direction DirectionOverride { get => _overrideDirection; set => _overrideDirection = value; }

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public bool EnableDirectionOverride { get => _enableOverrideDirection; set => _enableOverrideDirection = value; }

        // Sprite rendering path
        internal void Render(DrawingHandleWorld drawingHandle, Angle eyeRotation, in Angle worldRotation, in Vector2 worldPosition)
        {
            Direction? overrideDir = null;
            if (_enableOverrideDirection)
            {
                overrideDir = _overrideDirection;
            }

            RenderInternal(drawingHandle, eyeRotation, worldRotation, worldPosition, overrideDir);
        }

        private void RenderInternal(DrawingHandleWorld drawingHandle, Angle eyeRotation, Angle worldRotation, Vector2 worldPosition, Direction? overrideDirection)
        {
            // Reduce the angles to fix math shenanigans
            worldRotation = worldRotation.Reduced();

            if (worldRotation.Theta < 0)
                worldRotation = new Angle(worldRotation.Theta + Math.Tau);

            // worldRotation + eyeRotation should be the angle of the entity on-screen. If no-rot is enabled this is just set to zero.
            // However, at some point later the eye-matrix is applied separately, so we subtract -eye rotation for now:
            var entityMatrix = Matrix3.CreateTransform(worldPosition, NoRotation ? -eyeRotation : worldRotation);

            Matrix3.Multiply(ref LocalMatrix, ref entityMatrix, out var transform);

            var angle = worldRotation + eyeRotation; // angle on-screen. Used to decide the direction of 4/8 directional RSIs
            foreach (var layer in Layers)
            {
                layer.Render(drawingHandle, ref transform, angle, overrideDirection);
            }
        }

        public static Angle CalcRectWorldAngle(Angle worldAngle, int numDirections)
        {
            var theta = worldAngle.Theta;
            var segSize = (Math.PI * 2) / (numDirections * 2);
            var segments = (int)(theta / segSize);
            var odd = segments % 2;
            var result = theta - (segments * segSize) - (odd * segSize);

            return result;
        }

        public int GetLayerDirectionCount(ISpriteLayer layer)
        {
            if (!layer.RsiState.IsValid)
                return 1;

            // Pull texture from RSI state instead.
            var rsi = layer.Rsi ?? BaseRSI;
            if (rsi == null || !rsi.TryGetState(layer.RsiState, out var state))
            {
                state = GetFallbackState(resourceCache);
            }

            return state.Directions switch
            {
                RSI.State.DirectionType.Dir1 => 1,
                RSI.State.DirectionType.Dir4 => 4,
                RSI.State.DirectionType.Dir8 => 8,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public void FrameUpdate(float delta)
        {
            foreach (var t in Layers)
            {
                var layer = t;
                // Since StateId is a struct, we can't null-check it directly.
                if (!layer.State.IsValid || !layer.Visible || !layer.AutoAnimated)
                {
                    continue;
                }

                var rsi = layer.RSI ?? BaseRSI;
                if (rsi == null || !rsi.TryGetState(layer.State, out var state))
                {
                    state = GetFallbackState(resourceCache);
                }

                if (!state.IsAnimated)
                {
                    continue;
                }

                layer.AnimationTime += delta;
                layer.AnimationTimeLeft -= delta;
                _advanceFrameAnimation(layer, state);
            }
        }

        private static void _advanceFrameAnimation(Layer layer, RSI.State state)
        {
            var delayCount = state.DelayCount;
            while (layer.AnimationTimeLeft < 0)
            {
                layer.AnimationFrame += 1;

                if (layer.AnimationFrame >= delayCount)
                {
                    layer.AnimationFrame = 0;
                    layer.AnimationTime = -layer.AnimationTimeLeft;
                }

                layer.AnimationTimeLeft += state.GetDelay(layer.AnimationFrame);
            }
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState == null)
                return;

            var thestate = (SpriteComponentState)curState;

            Visible = thestate.Visible;
            DrawDepth = thestate.DrawDepth;
            Color = thestate.Color;
            RenderOrder = thestate.RenderOrder;

            scale = thestate.Scale;
            rotation = thestate.Rotation;
            offset = thestate.Offset;
            UpdateLocalMatrix();

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
            Layers.Clear();
            for (var i = 0; i < thestate.Layers.Count; i++)
            {
                var netlayer = thestate.Layers[i];
                var layer = new Layer(this)
                {
                    // These are easy so do them here.
                    _scale = netlayer.Scale,
                    _rotation = netlayer.Rotation,
                    _offset = netlayer.Offset,
                    Visible = netlayer.Visible,
                    Color = netlayer.Color
                };
                layer.UpdateLocalMatrix();
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

        private void QueueUpdateIsInert()
        {
            // Look this was an easy way to get bounds checks for layer updates.
            // If you really want it optimal you'll need to comb through all 2k lines of spritecomponent.
            if ((Owner != default ? entities : null)?.EventBus != null)
                UpdateBounds();

            if (_inertUpdateQueued)
                return;

            _inertUpdateQueued = true;
            // Yes that null check is valid because of that stupid fucking dummy IEntity.
            // Who thought that was a good idea.
            (Owner != default ? entities : null)?.EventBus?.RaiseEvent(EventSource.Local, new SpriteUpdateInertEvent {Sprite = this});
        }

        internal void DoUpdateIsInert()
        {
            _inertUpdateQueued = false;
            IsInert = true;

            foreach (var layer in Layers)
            {
                // Since StateId is a struct, we can't null-check it directly.
                if (!layer.State.IsValid || !layer.Visible || !layer.AutoAnimated)
                {
                    continue;
                }

                var rsi = layer.RSI ?? BaseRSI;
                if (rsi == null || !rsi.TryGetState(layer.State, out var state))
                {
                    state = GetFallbackState(resourceCache);
                }

                if (state.IsAnimated)
                {
                    IsInert = false;
                    break;
                }
            }
        }

        internal static RSI.State GetFallbackState(IResourceCache cache)
        {
            var rsi = cache.GetResource<RSIResource>("/Textures/error.rsi").RSI;
            return rsi["error"];
        }

        public string GetDebugString()
        {
            var builder = new StringBuilder();
            builder.AppendFormat(
                "vis/depth/scl/rot/ofs/col/norot/override/dir: {0}/{1}/{2}/{3}/{4}/{5}/{6}/{8}/{7}\n",
                Visible, DrawDepth, Scale, Rotation, Offset,
                Color, NoRotation, entities.GetComponent<TransformComponent>(Owner).WorldRotation.ToRsiDirection(RSI.State.DirectionType.Dir8),
                DirectionOverride
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

        /// <inheritdoc/>
        public Box2Rotated CalculateRotatedBoundingBox(Vector2 worldPosition, Angle worldRotation, IEye? eye = null)
        {
            // fast check for empty sprites
            if (!Visible || Layers.Count == 0)
            {
                return new Box2Rotated(new Box2(worldPosition, worldPosition), Angle.Zero, worldPosition);
            }

            // We need to modify world rotation so that it lies between 0 and 2pi.
            // This matters for 4 or 8 directional sprites deciding which quadrant (octant?) they lie in.
            // the 0->2pi convention is set by the sprite-rendering code that selects the layers.
            // See RenderInternal().

            worldRotation = worldRotation.Reduced();
            if (worldRotation.Theta < 0)
                worldRotation = new Angle(worldRotation.Theta + Math.Tau);

            eye ??= eyeManager.CurrentEye;

            // Need relative angle on screen for determining the sprite rsi direction.
            Angle relativeRotation = NoRotation
                ? Angle.Zero
                : worldRotation + eye.Rotation; 

            // we need to calculate bounding box taking into account all nested layers
            // because layers can have offsets, scale or rotation, we need to calculate a new BB
            // based on lowest bottomLeft and highest topRight points from all layers
            var box = Layers[0].CalculateBoundingBox(relativeRotation);

            for (int i = 1; i < Layers.Count; i++)
            {
                var layer = Layers[i];
                if (!layer.Visible) continue;
                var layerBB = layer.CalculateBoundingBox(relativeRotation);

                box = box.Union(layerBB);
            }

            // Next, what we should do is take box2 and apply the sprites LocalMatrix, and then apply the entity's matrix
            // BUT Matrix3.TransformBox only does bounding boxes. So we transform our box manually into a rotated box:
            if (Scale != Vector2.One)
                box = box.Scale(Scale);

            var adjustedOffset = NoRotation
                ? (-eye.Rotation).RotateVec(Offset)
                : worldRotation.RotateVec(Offset);

            Vector2 position = adjustedOffset + worldPosition;
            Angle finalRotation = NoRotation
                ? Rotation - eye.Rotation
                : Rotation + worldRotation;

            return new Box2Rotated(box.Translated(position), finalRotation, position);
        }

        internal void UpdateBounds()
        {
            entities.EventBus.RaiseLocalEvent(Owner, new SpriteUpdateEvent());
        }

        /// <summary>
        ///     Enum to "offset" a cardinal direction.
        /// </summary>
        public enum DirectionOffset : byte
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

        public sealed class Layer : ISpriteLayer, ISerializationHooks
        {
            [ViewVariables] private readonly SpriteComponent _parent;

            [ViewVariables] public ShaderInstance? Shader;
            [ViewVariables] public Texture? Texture;

            [ViewVariables] public RSI? RSI;
            [ViewVariables] public RSI.StateId State;
            [ViewVariables] public float AnimationTimeLeft;
            [ViewVariables] public float AnimationTime;
            [ViewVariables] public int AnimationFrame;

            public Matrix3 LocalMatrix = Matrix3.Identity;

            [ViewVariables(VVAccess.ReadWrite)]
            public Vector2 Scale
            {
                get => _scale;
                set
                {
                    if (_scale.EqualsApprox(value)) return;

                    _scale = value;
                    UpdateLocalMatrix();
                    _parent.UpdateBounds();
                }
            }
            internal Vector2 _scale = Vector2.One;

            [ViewVariables(VVAccess.ReadWrite)]
            public Angle Rotation
            {
                get => _rotation;
                set
                {
                    if (_rotation.EqualsApprox(value)) return;

                    _rotation = value;
                    UpdateLocalMatrix();
                    _parent.UpdateBounds();
                }
            }
            internal Angle _rotation = Angle.Zero;

            [ViewVariables(VVAccess.ReadWrite)]
            public bool Visible = true;

            [ViewVariables(VVAccess.ReadWrite)]
            public Color Color { get; set; } = Color.White;

            [ViewVariables(VVAccess.ReadWrite)]
            public bool AutoAnimated = true;

            [ViewVariables(VVAccess.ReadWrite)]
            public Vector2 Offset
            {
                get => _offset;
                set
                {
                    if (_offset.EqualsApprox(value)) return;

                    _offset = value;
                    UpdateLocalMatrix();
                    _parent.UpdateBounds();
                }
            }

            internal Vector2 _offset;

            [ViewVariables]
            public DirectionOffset DirOffset { get; set; }

            [ViewVariables]
            public RSI? ActualRsi => RSI ?? _parent.BaseRSI;

            public Layer(SpriteComponent parent)
            {
                _parent = parent;
            }

            public Layer(Layer toClone, SpriteComponent parentSprite)
            {
                _parent = parentSprite;
                if (toClone.Shader != null)
                {
                    Shader = toClone.Shader.Mutable ? toClone.Shader.Duplicate() : toClone.Shader;
                }
                Texture = toClone.Texture;
                RSI = toClone.RSI;
                State = toClone.State;
                AnimationTimeLeft = toClone.AnimationTimeLeft;
                AnimationTime = toClone.AnimationTime;
                AnimationFrame = toClone.AnimationFrame;
                Visible = toClone.Visible;
                Color = toClone.Color;
                DirOffset = toClone.DirOffset;
                AutoAnimated = toClone.AutoAnimated;

                _scale = toClone.Scale;
                _rotation = toClone.Rotation;
                _offset = toClone.Offset;
                UpdateLocalMatrix();
            }

            void ISerializationHooks.AfterDeserialization()
            {
                UpdateLocalMatrix();
            }

            internal void UpdateLocalMatrix()
            {
                LocalMatrix = Matrix3.CreateTransform(in _offset, in _rotation, in _scale);
            }

            RSI? ISpriteLayer.Rsi { get => RSI; set => SetRsi(value); }
            RSI.StateId ISpriteLayer.RsiState { get => State; set => SetState(value); }
            Texture? ISpriteLayer.Texture { get => Texture; set => SetTexture(value); }

            public PrototypeLayerData ToPrototypeData()
            {
                return new PrototypeLayerData
                {
                    Color = Color,
                    Rotation = Rotation,
                    Scale = Scale,
                    //todo Shader = Shader,
                    State = State.Name,
                    Visible = Visible,
                    RsiPath = RSI?.Path?.ToString(),
                    //todo TexturePath = Textur
                    //todo MapKeys
                };
            }

            bool ISpriteLayer.Visible
            {
                get => Visible;
                set => SetVisible(value);
            }

            float ISpriteLayer.AnimationTime
            {
                get => AnimationTime;
                set => SetAnimationTime(value);
            }

            int ISpriteLayer.AnimationFrame => AnimationFrame;

            bool ISpriteLayer.AutoAnimated
            {
                get => AutoAnimated;
                set => SetAutoAnimated(value);
            }

            public RSI.State.Direction EffectiveDirection(Angle worldRotation)
            {
                if (State == default)
                {
                    return default;
                }

                var rsi = ActualRsi;
                if (rsi == null)
                {
                    return default;
                }

                if (rsi.TryGetState(State, out var state))
                {
                    return EffectiveDirection(state, worldRotation, null);
                }

                return default;
            }

            public RSI.State.Direction EffectiveDirection(RSI.State state, Angle worldRotation,
                Direction? overrideDirection)
            {
                if (state.Directions == RSI.State.DirectionType.Dir1)
                {
                    return RSI.State.Direction.South;
                }
                else
                {
                    RSI.State.Direction dir;
                    if (overrideDirection != null)
                    {
                        dir = overrideDirection.Value.Convert(state.Directions);
                    }
                    else
                    {
                        dir = worldRotation.ToRsiDirection(state.Directions);
                    }

                    return dir.OffsetRsiDir(DirOffset);
                }
            }

            public void SetAnimationTime(float animationTime)
            {
                if (!State.IsValid)
                {
                    return;
                }

                var theLayerRSI = ActualRsi;
                if (theLayerRSI == null)
                {
                    return;
                }

                var state = theLayerRSI[State];
                if (animationTime > AnimationTime)
                {
                    // Handle advancing differently from going backwards.
                    AnimationTimeLeft -= (animationTime - AnimationTime);
                }
                else
                {
                    // Going backwards we re-calculate from zero.
                    // Definitely possible to optimize this for going backwards but I'm too lazy to figure that out.
                    AnimationTimeLeft = -animationTime + state.GetDelay(0);
                    AnimationFrame = 0;
                }

                AnimationTime = animationTime;
                // After setting timing data correctly, run advance to get to the correct frame.
                _advanceFrameAnimation(this, state);
            }

            public void SetAutoAnimated(bool value)
            {
                AutoAnimated = value;

                _parent.QueueUpdateIsInert();
            }

            public void SetVisible(bool value)
            {
                Visible = value;

                _parent.QueueUpdateIsInert();
            }

            public void SetRsi(RSI? rsi)
            {
                RSI = rsi;
                if (!State.IsValid)
                {
                    return;
                }

                // Gotta do this because somebody might use null as argument (totally valid).
                var actualRsi = ActualRsi;
                if (actualRsi == null)
                {
                    Logger.ErrorS(LogCategory, "No RSI to pull new state from! Trace:\n{0}", Environment.StackTrace);
                    Texture = null;
                }
                else
                {
                    if (actualRsi.TryGetState(State, out var state))
                    {
                        AnimationTimeLeft = state.GetDelay(0);
                    }
                    else
                    {
                        Logger.ErrorS(LogCategory, "State '{0}' does not exist in set RSI. Trace:\n{1}", State,
                            Environment.StackTrace);
                        Texture = null;
                    }
                }

                _parent.QueueUpdateIsInert();
            }

            public void SetState(RSI.StateId stateId)
            {
                if (State == stateId)
                {
                    return;
                }

                State = stateId;
                RSI.State? state;
                var rsi = ActualRsi;
                if (rsi == null)
                {
                    state = GetFallbackState(_parent.resourceCache);
                    Logger.ErrorS(LogCategory, "No RSI to pull new state from! Trace:\n{0}", Environment.StackTrace);
                }
                else
                {
                    if (!rsi.TryGetState(stateId, out state))
                    {
                        state = GetFallbackState(_parent.resourceCache);
                        Logger.ErrorS(LogCategory, "State '{0}' does not exist in RSI. Trace:\n{1}", stateId,
                            Environment.StackTrace);
                    }
                }

                AnimationFrame = 0;
                AnimationTime = 0;
                AnimationTimeLeft = state.GetDelay(0);

                _parent.QueueUpdateIsInert();
            }

            public void SetTexture(Texture? texture)
            {
                State = default;
                Texture = texture;

                _parent.QueueUpdateIsInert();
            }

            /// <inheritdoc/>
            public Vector2i PixelSize
            {
                get
                {
                    var pixelSize = Vector2i.Zero;
                    if (Texture != null)
                    {
                        pixelSize = Texture.Size;
                    }
                    else if (ActualRsi != null)
                    {
                        pixelSize = ActualRsi.Size;
                    }

                    return pixelSize;
                }
            }

            /// <inheritdoc/>
            public Box2 CalculateBoundingBox(Angle angle)
            {
                // Other than some special cases for simple layers, this will basically just apply the matrix obtained
                // via GetLayerDrawMatrix() to this layer's bounding box.

                var rsiState = GetActualState();
                var dir = GetDirection(rsiState, angle);

                // special case for simple layers. The vast majority of layers are like this.
                if (_rotation == Angle.Zero)
                {
                    var textureSize = PixelSize / EyeManager.PixelsPerMeter;

                    // this is basically the "rsiDirectionMatrix" but done explicitly.
                    var box = dir switch
                    {
                        RSIDirection.South or RSIDirection.North => Box2.CenteredAround(Offset, textureSize),
                        RSIDirection.East or RSIDirection.West => Box2.CenteredAround(Offset, (textureSize.Y, textureSize.X)), // rotate 90 degrees
                        _ => Box2.CenteredAround(Offset, Vector2.One * (textureSize.X + textureSize.Y) / MathF.Sqrt(2))
                    };

                    return _scale == Vector2.One ? box : box.Scale(_scale);
                }

                // welp if we have arbitrary rotation, lets just apply general layer transform;
                Matrix3 layerDrawMatrix;
                if (_parent.NoRotation)
                {
                    layerDrawMatrix = LocalMatrix;
                }
                else
                {
                    var rsiDirectionMatrix = Matrix3.CreateTransform(Vector2.Zero, -dir.Convert().ToAngle());
                    Matrix3.Multiply(ref rsiDirectionMatrix, ref LocalMatrix, out layerDrawMatrix);
                }

                return layerDrawMatrix.TransformBox(Box2.CentredAroundZero(PixelSize / EyeManager.PixelsPerMeter));
            }

            internal RSI.State? GetActualState()
            {
                if (!State.IsValid)
                    return null;

                // Pull texture from RSI state
                var rsi = RSI ?? _parent.BaseRSI;
                if (rsi == null || !rsi.TryGetState(State, out var rsiState))
                {
                    rsiState = GetFallbackState(_parent.resourceCache);
                }

                return rsiState;
            }

            /// <summary>
            ///     Get RSI state direction based on an entity's apparent angle on screen (world rotation + eye rotation)
            /// </summary
            internal RSIDirection GetDirection(RSI.State? state, Angle angle)
            {
                if (state == null || state.Directions == RSI.State.DirectionType.Dir1)
                    return RSIDirection.South;

                return angle.ToRsiDirection(state.Directions);
            }

            /// <summary>
            ///     Given the apparent rotation of an entity on screen (world + eye rotation), get layer's matrix for drawing &
            ///     relevant RSI direction.
            /// </summary>
            public void GetLayerDrawMatrix(Angle angle, out Matrix3 layerDrawMatrix, out RSIDirection dir, out RSI.State? rsiState)
            {
                rsiState = GetActualState();
                dir = GetDirection(rsiState, angle);

                if (_parent.NoRotation)
                {
                    layerDrawMatrix = LocalMatrix;
                }
                else
                {
                    var rsiDirectionMatrix = Matrix3.CreateTransform(Vector2.Zero, -dir.Convert().ToAngle());
                    Matrix3.Multiply(ref rsiDirectionMatrix, ref LocalMatrix, out layerDrawMatrix);
                }
            }

            internal void Render(DrawingHandleWorld drawingHandle, ref Matrix3 spriteMatrix, Angle angle, Direction? overrideDirection)
            {
                if (!Visible)
                    return;

                GetLayerDrawMatrix(angle,
                    out var layerMatrix,
                    out var dir,
                    out var state);

                Matrix3.Multiply(ref layerMatrix, ref spriteMatrix, out var transformMatrix);
                drawingHandle.SetTransform(in transformMatrix);

                // The actual drawn direction can differ from the one that the angle would usually result in
                if (overrideDirection != null && state != null)
                    dir = overrideDirection.Value.Convert(state.Directions);
                dir = dir.OffsetRsiDir(DirOffset);

                // get the correct directional texture from the state
                var texture = GetRenderTexture(state, dir);

                // and draw it.
                RenderTexture(drawingHandle, texture);
            }

            private void RenderTexture(DrawingHandleWorld drawingHandle, Texture texture)
            {
                if (Shader != null)
                    drawingHandle.UseShader(Shader);

                var layerColor = _parent.color * Color;

                var position = -(Vector2)texture.Size / (2f * EyeManager.PixelsPerMeter);
                var textureSize = texture.Size / (float)EyeManager.PixelsPerMeter;
                var quad = Box2.FromDimensions(position, textureSize);

                drawingHandle.DrawTextureRectRegion(texture, quad, layerColor);

                if (Shader != null)
                    drawingHandle.UseShader(null);
            }

            private Texture GetRenderTexture(RSI.State? state, RSIDirection dir)
            {
                if (state == null)
                    return Texture ?? _parent.resourceCache.GetFallback<TextureResource>().Texture;

                return state.GetFrame(dir, AnimationFrame);
            }
        }

        void IAnimationProperties.SetAnimatableProperty(string name, object value)
        {
            if (!name.StartsWith("layer/"))
            {
                AnimationHelper.SetAnimatableProperty(this, name, value);
                return;
            }

            var delimiter = name.IndexOf("/", 6, StringComparison.Ordinal);
            var indexString = name.Substring(6, delimiter - 6);
            var index = int.Parse(indexString, CultureInfo.InvariantCulture);
            var layerProp = name.Substring(delimiter + 1);

            switch (layerProp)
            {
                case "texture":
                    LayerSetTexture(index, (string)value);
                    return;
                case "state":
                    LayerSetState(index, (string)value);
                    return;
                case "color":
                    LayerSetColor(index, (Color)value);
                    return;
                default:
                    throw new ArgumentException($"Unknown layer property '{layerProp}'");
            }
        }

        public IRsiStateLike? Icon
        {
            get
            {
                if (Layers.Count == 0) return null;

                var layer = Layers[0];

                var texture = layer.Texture;

                if (!layer.State.IsValid) return texture;

                // Pull texture from RSI state instead.
                var rsi = layer.RSI ?? BaseRSI;
                if (rsi == null || !rsi.TryGetState(layer.State, out var state))
                {
                    state = GetFallbackState(resourceCache);
                }

                return state;
            }
        }

        public static IEnumerable<IDirectionalTextureProvider> GetPrototypeTextures(EntityPrototype prototype, IResourceCache resourceCache)
        {
            return GetPrototypeTextures(prototype, resourceCache, out var _);
        }

        public static IEnumerable<IDirectionalTextureProvider> GetPrototypeTextures(EntityPrototype prototype, IResourceCache resourceCache, out bool noRot)
        {
            var results = new List<IDirectionalTextureProvider>();
            noRot = false;
            var icon = IconComponent.GetPrototypeIcon(prototype, resourceCache);
            if (icon != null)
            {
                results.Add(icon);
                return results;
            }

            if (!prototype.Components.TryGetValue("Sprite", out _))
            {
                results.Add(resourceCache.GetFallback<TextureResource>().Texture);
                return results;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var dummy = entityManager.SpawnEntity(prototype.ID, MapCoordinates.Nullspace);
            var spriteComponent = entityManager.EnsureComponent<SpriteComponent>(dummy);
            EntitySystem.Get<AppearanceSystem>().OnChangeData(dummy);

            var anyTexture = false;
            foreach (var layer in spriteComponent.AllLayers)
            {
                if (layer.Texture != null)
                    results.Add(layer.Texture);
                if (!layer.RsiState.IsValid || !layer.Visible) continue;

                var rsi = layer.Rsi ?? spriteComponent.BaseRSI;
                if (rsi == null ||
                    !rsi.TryGetState(layer.RsiState, out var state))
                    continue;

                results.Add(state);
                anyTexture = true;
            }

            noRot = spriteComponent.NoRotation;

            entityManager.DeleteEntity(dummy);

            if (!anyTexture)
                results.Add(resourceCache.GetFallback<TextureResource>().Texture);
            return results;
        }

        public static IRsiStateLike GetPrototypeIcon(EntityPrototype prototype, IResourceCache resourceCache)
        {
            var icon = IconComponent.GetPrototypeIcon(prototype, resourceCache);
            if (icon != null) return icon;

            if (!prototype.Components.ContainsKey("Sprite"))
            {
                return GetFallbackState(resourceCache);
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var dummy = entityManager.SpawnEntity(prototype.ID, MapCoordinates.Nullspace);
            var spriteComponent = entityManager.EnsureComponent<SpriteComponent>(dummy);
            var result = spriteComponent.Icon ?? GetFallbackState(resourceCache);
            entityManager.DeleteEntity(dummy);

            return result;
        }
    }

    internal sealed class SpriteUpdateEvent : EntityEventArgs
    {

    }

    internal struct SpriteUpdateInertEvent
    {
        public SpriteComponent Sprite;
    }
}
