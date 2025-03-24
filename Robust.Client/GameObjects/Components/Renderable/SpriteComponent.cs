using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Clyde;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared.Animations;
using Robust.Shared.ComponentTrees;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using static Robust.Client.ComponentTrees.SpriteTreeSystem;
using DrawDepthTag = Robust.Shared.GameObjects.DrawDepth;
using static Robust.Shared.Serialization.TypeSerializers.Implementations.SpriteSpecifierSerializer;
using Direction = Robust.Shared.Maths.Direction;
using Vector4 = Robust.Shared.Maths.Vector4;
using SysVec4 = System.Numerics.Vector4;

namespace Robust.Client.GameObjects
{
    [RegisterComponent]
    public sealed partial class SpriteComponent : Component, IComponentDebug, ISerializationHooks, IComponentTreeEntry<SpriteComponent>, IAnimationProperties
    {
        [Dependency] private readonly IResourceCache resourceCache = default!;
        [Dependency] private readonly IPrototypeManager prototypes = default!;
        [Dependency] private readonly IEntityManager entities = default!;
        [Dependency] private readonly IReflectionManager reflection = default!;
        [Dependency] private readonly IEyeManager eyeManager = default!;
        [Dependency] private readonly IComponentFactory factory = default!;

        /// <summary>
        ///     See <see cref="CVars.RenderSpriteDirectionBias"/>.
        /// </summary>
        public static double DirectionBias = -0.05;

        /// <summary>
        ///     Whether the layers have independant drawing strategies, e.g some may snap to cardinals while others won't.
        ///     The sprite should still set its global rendering method (e.g NoRot or SnapCardinals), this only gives finer control over how layers are rendered internally.
        /// </summary>
        [DataField("granularLayersRendering")]
        public bool GranularLayersRendering = false;

        [DataField("visible")]
        private bool _visible = true;

        // VV convenience variable to examine layer objects using layer keys
        [ViewVariables]
        private Dictionary<object, Layer> _mappedLayers => LayerMap.ToDictionary(x => x.Key, x => Layers[x.Value]);

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Visible
        {
            get => _visible;
            set
            {
                if (_visible == value) return;
                _visible = value;

                QueueUpdateRenderTree();
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
                if (MathF.Abs(value.X) < 0.005f || MathF.Abs(value.Y) < 0.005f)
                {
                    // Scales of ~0.0025 or lower can lead to singular matrices due to rounding errors.
                    Logger.Error($"Attempted to set layer sprite scale to very small values. Entity: {entities.ToPrettyString(Owner)}. Scale: {value}");
                    return;
                }

                _bounds = _bounds.Scale(value / scale);
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

        public Matrix3x2 LocalMatrix = Matrix3x2.Identity;

        [Animatable]
        [ViewVariables(VVAccess.ReadWrite)]
        public Color Color
        {
            get => color;
            set => color = value;
        }

        [ViewVariables]
        public DynamicTree<ComponentTreeEntry<SpriteComponent>>? Tree { get; set; }

        public EntityUid? TreeUid { get; set; }

        public bool AddToTree => Visible && !ContainerOccluded && Layers.Count > 0;

        public bool TreeUpdateQueued { get; set; }

        private RSI? _baseRsi;

        [ViewVariables(VVAccess.ReadWrite)]
        public RSI? BaseRSI
        {
            get => _baseRsi;
            set
            {
                if (value == _baseRsi)
                    return;

                _baseRsi = value;
                if (value == null)
                    return;

                for (var i = 0; i < Layers.Count; i++)
                {
                    var layer = Layers[i];
                    if (!layer.State.IsValid || layer.RSI != null)
                    {
                        continue;
                    }

                    layer.UpdateActualState();

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

        /// <summary>
        ///     Should this entity show up in containers regardless of whether the container can show contents?
        /// </summary>
        [DataField("overrideContainerOcclusion")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool OverrideContainerOcclusion;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ContainerOccluded
        {
            get => _containerOccluded && !OverrideContainerOcclusion;
            set
            {
                if (_containerOccluded == value) return;
                _containerOccluded = value;
                QueueUpdateRenderTree();
            }
        }

        private bool _containerOccluded;

        private Box2 _bounds;

        /// <summary>
        ///     The bounds of the sprite. This does factor in the sprite's <see cref="Scale"/> but not the
        ///     <see cref="Rotation"/> and <see cref="Offset"/>
        /// </summary>
        public Box2 Bounds => _bounds;

        [ViewVariables(VVAccess.ReadWrite)] internal bool _inertUpdateQueued;

        /// <summary>
        ///     Shader instance to use when drawing the final sprite to the world.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public ShaderInstance? PostShader { get; set; }

        /// <summary>
        ///     Whether or not to pass the screen texture to the <see cref="PostShader"/>.
        /// </summary>
        /// <remarks>
        ///     Should be false unless you really need it.
        /// </remarks>
        [DataField("getScreenTexture")]
        [ViewVariables(VVAccess.ReadWrite)]
        private bool _getScreenTexture = false;
        public bool GetScreenTexture
        {
            get => _getScreenTexture && PostShader != null;
            set => _getScreenTexture = value;
        }

        /// <summary>
        ///     If true, this raise a entity system event before rendering this sprite, allowing systems to modify the
        ///     shader parameters. Usually this can just be done via a frame-update, but some shaders require
        ///     information about the viewport / eye.
        /// </summary>
        [DataField("raiseShaderEvent")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool RaiseShaderEvent = false;

        [ViewVariables] private Dictionary<object, int> LayerMap = new();
        [ViewVariables] private bool _layerMapShared;
        [ViewVariables] internal List<Layer> Layers = new();

        [ViewVariables(VVAccess.ReadWrite)] public uint RenderOrder { get; set; }

        public const string LogCategory = "go.comp.sprite";

        [ViewVariables(VVAccess.ReadWrite)] public bool IsInert { get; internal set; }

        void ISerializationHooks.AfterDeserialization()
        {
            // Please somebody burn this to the ground. There is so much spaghetti.

            IoCManager.InjectDependencies(this);

            {
                if (!string.IsNullOrWhiteSpace(rsi))
                {
                    var rsiPath = TextureRoot / rsi;
                    if(resourceCache.TryGetResource(rsiPath, out RSIResource? resource))
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
                        RenderingStrategy = LayerRenderingStrategy.UseSpriteStrategy,
                        Cycle = false,
                    });
                    state = null;
                    texture = null;
                }
            }

            if (layerDatums.Count != 0)
            {
                LayerMap.Clear();
                Layers.Clear();
                foreach (var datum in layerDatums)
                {
                    AddLayer(datum);
                }

                _layerMapShared = true;

                QueueUpdateRenderTree();
                QueueUpdateIsInert();
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
            _bounds = other._bounds;
            _visible = other._visible;
            _layerMapShared = other._layerMapShared;
            color = other.color;
            offset = other.offset;
            rotation = other.rotation;
            scale = other.scale;
            UpdateLocalMatrix();
            drawDepth = other.drawDepth;
            _screenLock = other._screenLock;
            DirectionOverride = other.DirectionOverride;
            EnableDirectionOverride = other.EnableDirectionOverride;
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
            GranularLayersRendering = other.GranularLayersRendering;
        }

        internal void UpdateLocalMatrix()
        {
            LocalMatrix = Matrix3Helpers.CreateTransform(in offset, in rotation, in scale);
        }

        public Matrix3x2 GetLocalMatrix()
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
        public bool LayerMapTryGet(object key, out int layer, bool logError = false)
        {
            var result = LayerMap.TryGetValue(key, out layer);

            if (!result && logError)
            {
                Logger.ErrorS(LogCategory, "{0} - Layer with key '{1}' does not exist! Trace:\n{2}",
                    entities.ToPrettyString(Owner), key, Environment.StackTrace);
            }

            return result;
        }

        public bool TryGetLayer(int index, [NotNullWhen(true)] out Layer? layer, bool logError = false)
        {
            if (index < Layers.Count)
            {
                layer = Layers[index];
                return true;
            }

            if (logError)
            {
                Logger.ErrorS(LogCategory, "{0} - Layer index '{1}' does not exist! Trace:\n{2}",
                    entities.ToPrettyString(Owner), index, Environment.StackTrace);
            }

            layer = null;
            return false;
        }

        public bool LayerExists(int layer, bool logError = true) => TryGetLayer(layer, out _, logError);
        public bool LayerExists(object key, bool logError = false) => LayerMapTryGet(key, out _, logError);

        private void _layerMapEnsurePrivate()
        {
            if (!_layerMapShared)
            {
                return;
            }

            LayerMap = LayerMap.ShallowClone();
            _layerMapShared = false;
        }

        public int LayerMapReserveBlank(object key)
        {
            if (LayerMapTryGet(key, out var index))
            {
                return index;
            }

            index = AddBlankLayer();
            LayerMapSet(key, index);

            return index;
        }

        public int AddBlankLayer(int? newIndex = null)
        {
            var layer = new Layer(this);
            return AddLayer(layer, newIndex);
        }

        /// <summary>
        ///     Add a new layer based on some <see cref="PrototypeLayerData"/>.
        /// </summary>
        public int AddLayer(PrototypeLayerData layerDatum, int? newIndex = null)
        {
            var layer = new Layer(this);

            var index = AddLayer(layer, newIndex);

            LayerSetData(index, layerDatum);
            return index;
        }

        public int AddLayer(string texturePath, int? newIndex = null)
        {
            return AddLayer(new ResPath(texturePath), newIndex);
        }

        public int AddLayer(ResPath texturePath, int? newIndex = null)
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
            return AddLayer(stateId, new ResPath(rsiPath), newIndex);
        }

        public int AddLayerState(string stateId, string rsiPath, int? newIndex = null)
        {
            return AddLayer(new RSI.StateId(stateId), rsiPath, newIndex);
        }

        public int AddLayer(RSI.StateId stateId, ResPath rsiPath, int? newIndex = null)
        {
            if (!resourceCache.TryGetResource<RSIResource>(TextureRoot / rsiPath, out var res))
            {
                Logger.ErrorS(LogCategory, "Unable to load RSI '{0}'. Trace:\n{1}", rsiPath, Environment.StackTrace);
            }

            return AddLayer(stateId, res?.RSI, newIndex);
        }

        public int AddLayerState(string stateId, ResPath rsiPath, int? newIndex = null)
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

            RebuildBounds();
            QueueUpdateIsInert();
            return index;
        }

        public void RemoveLayer(int layer)
        {
            if (!LayerExists(layer))
                return;

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

            RebuildBounds();
            QueueUpdateIsInert();
        }

        public void RemoveLayer(object layerKey)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            RemoveLayer(layer);
        }

        private void RebuildBounds()
        {
            _bounds = new Box2();
            foreach (var layer in Layers)
            {
                if (!layer.Visible || layer.Blank) continue;

                _bounds = _bounds.Union(layer.CalculateBoundingBox());
            }
            _bounds = _bounds.Scale(Scale);
            QueueUpdateRenderTree();
        }

        /// <summary>
        ///     Fills in a layer's values using some <see cref="PrototypeLayerData"/>.
        /// </summary>
        public void LayerSetData(int index, PrototypeLayerData layerDatum)
        {
            if (!TryGetLayer(index, out var layer))
                return;

            if (!string.IsNullOrWhiteSpace(layerDatum.RsiPath))
            {
                var path = TextureRoot / layerDatum.RsiPath;

                if (resourceCache.TryGetResource(path, out RSIResource? resource))
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
                        layer.AnimationTime = 0;
                        layer.AnimationFrame = 0;
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

            if (layerDatum.Shader != null)
            {
                if (layerDatum.Shader == string.Empty)
                {
                    layer.ShaderPrototype = null;
                    layer.UnShaded = false;
                    layer.Shader = null;
                }
                else if (layerDatum.Shader == SpriteSystem.UnshadedId.Id)
                {
                    layer.ShaderPrototype = SpriteSystem.UnshadedId;
                    layer.UnShaded = true;
                    layer.Shader = null;
                }
                else if (prototypes.TryIndex<ShaderPrototype>(layerDatum.Shader, out var prototype))
                {
                    layer.ShaderPrototype = layerDatum.Shader;
                    layer.Shader = prototype.Instance();
                    layer.UnShaded = false;
                }
                else
                {
                    Logger.ErrorS(LogCategory,
                        "Shader prototype '{0}' does not exist.",
                        layerDatum.Shader);
                }
            }

            if (layerDatum.MapKeys != null)
            {
                foreach (var keyString in layerDatum.MapKeys)
                {
                    var key = ParseKey(keyString);

                    if (LayerMap.TryGetValue(key, out var mappedIndex))
                    {
                        if (mappedIndex != index)
                            Logger.ErrorS(LogCategory, "Duplicate layer map key definition: {0}", key);
                        continue;
                    }

                    _layerMapEnsurePrivate();
                    LayerMap[key] = index;
                }
            }

            layer.RenderingStrategy = layerDatum.RenderingStrategy ?? layer.RenderingStrategy;
            layer.Cycle = layerDatum.Cycle;

            layer.Color = layerDatum.Color ?? layer.Color;
            layer._rotation = layerDatum.Rotation ?? layer._rotation;
            layer._offset = layerDatum.Offset ?? layer._offset;
            layer._scale = layerDatum.Scale ?? layer._scale;
            layer.UpdateLocalMatrix();

            // If neither state: nor texture: were provided we assume that they want a blank invisible layer.
            layer.Visible = layerDatum.Visible ?? layer.Visible;

            if (layerDatum.CopyToShaderParameters is { } copyParameters)
            {
                layer.CopyToShaderParameters = new CopyToShaderParameters(ParseKey(copyParameters.LayerKey))
                {
                    ParameterTexture = copyParameters.ParameterTexture,
                    ParameterUV = copyParameters.ParameterUV
                };
            }
            else
            {
                layer.CopyToShaderParameters = null;
            }

            RebuildBounds();
        }

        private object ParseKey(string keyString)
        {
            if (reflection.TryParseEnumReference(keyString, out var @enum))
                return @enum;

            return keyString;
        }

        public void LayerSetData(object layerKey, PrototypeLayerData data)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetData(layer, data);
        }

        public void LayerSetShader(int layer, ShaderInstance? shader, string? prototype = null)
        {
            if (!TryGetLayer(layer, out var theLayer, true))
                return;

            if (shader == null)
            {
                theLayer.UnShaded = false;
                theLayer.Shader = null;
                theLayer.ShaderPrototype = null;
                return;
            }

            if (prototype == SpriteSystem.UnshadedId.Id)
            {
                theLayer.UnShaded = true;
                theLayer.ShaderPrototype = SpriteSystem.UnshadedId;
                theLayer.Shader = null;
                return;
            }

            theLayer.UnShaded = false;
            theLayer.Shader = shader;
            theLayer.ShaderPrototype = prototype;
        }

        public void LayerSetShader(object layerKey, ShaderInstance? shader, string? prototype = null)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetShader(layer, shader, prototype);
        }

        public void LayerSetShader(int layer, string shaderName)
        {
            if (!prototypes.TryIndex<ShaderPrototype>(shaderName, out var prototype))
            {
                Logger.ErrorS(LogCategory, "Shader prototype '{0}' does not exist. Trace:\n{1}", shaderName,
                    Environment.StackTrace);

                LayerSetShader(layer, null, null);
                return;
            }

            LayerSetShader(layer, prototype.Instance(), shaderName);
        }

        public void LayerSetShader(object layerKey, string shaderName)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetShader(layer, shaderName);
        }

        public void LayerSetSprite(int layer, SpriteSpecifier specifier)
        {
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
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetSprite(layer, specifier);
        }

        public void LayerSetTexture(int layer, Texture? texture)
        {
            if (!TryGetLayer(layer, out var theLayer, true))
                return;
            theLayer.SetTexture(texture);

            QueueUpdateIsInert();
            RebuildBounds();
        }

        public void LayerSetTexture(object layerKey, Texture texture)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetTexture(layer, texture);
        }

        public void LayerSetTexture(int layer, string texturePath)
        {
            LayerSetTexture(layer, new ResPath(texturePath));
        }

        public void LayerSetTexture(object layerKey, string texturePath)
        {
            LayerSetTexture(layerKey, new ResPath(texturePath));
        }

        public void LayerSetTexture(int layer, ResPath texturePath)
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

        public void LayerSetTexture(object layerKey, ResPath texturePath)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetTexture(layer, texturePath);
        }

        public void LayerSetState(int layer, RSI.StateId stateId)
        {
            if (!TryGetLayer(layer, out var theLayer, true))
                return;
            theLayer.SetState(stateId);
            RebuildBounds();
        }

        public void LayerSetState(object layerKey, RSI.StateId stateId)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetState(layer, stateId);
        }

        public void LayerSetState(int layer, RSI.StateId stateId, RSI? rsi)
        {
            if (!TryGetLayer(layer, out var theLayer, true))
                return;
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

            QueueUpdateIsInert();
            RebuildBounds();
        }

        public void LayerSetState(object layerKey, RSI.StateId stateId, RSI rsi)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetState(layer, stateId, rsi);
        }

        public void LayerSetState(int layer, RSI.StateId stateId, string rsiPath)
        {
            LayerSetState(layer, stateId, new ResPath(rsiPath));
        }

        public void LayerSetState(object layerKey, RSI.StateId stateId, string rsiPath)
        {
            LayerSetState(layerKey, stateId, new ResPath(rsiPath));
        }

        public void LayerSetState(int layer, RSI.StateId stateId, ResPath rsiPath)
        {
            if (!resourceCache.TryGetResource<RSIResource>(TextureRoot / rsiPath, out var res))
            {
                Logger.ErrorS(LogCategory, "Unable to load RSI '{0}'. Trace:\n{1}", rsiPath, Environment.StackTrace);
            }

            LayerSetState(layer, stateId, res?.RSI);
        }

        public void LayerSetState(object layerKey, RSI.StateId stateId, ResPath rsiPath)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetState(layer, stateId, rsiPath);
        }

        public void LayerSetRSI(int layer, RSI? rsi)
        {
            if (!TryGetLayer(layer, out var theLayer, true))
                return;
            theLayer.SetRsi(rsi);
            RebuildBounds();
        }

        public void LayerSetRSI(object layerKey, RSI rsi)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetRSI(layer, rsi);
        }

        public void LayerSetRSI(int layer, string rsiPath)
        {
            LayerSetRSI(layer, new ResPath(rsiPath));
        }

        public void LayerSetRSI(object layerKey, string rsiPath)
        {
            LayerSetRSI(layerKey, new ResPath(rsiPath));
        }

        public void LayerSetRSI(int layer, ResPath rsiPath)
        {
            if (!resourceCache.TryGetResource<RSIResource>(TextureRoot / rsiPath, out var res))
            {
                Logger.ErrorS(LogCategory, "Unable to load RSI '{0}'. Trace:\n{1}", rsiPath, Environment.StackTrace);
            }

            LayerSetRSI(layer, res?.RSI);
        }

        public void LayerSetRSI(object layerKey, ResPath rsiPath)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetRSI(layer, rsiPath);
        }

        public void LayerSetScale(int layer, Vector2 scale)
        {
            if (!TryGetLayer(layer, out var theLayer, true))
                return;
            theLayer.Scale = scale;
            RebuildBounds();
        }

        public void LayerSetScale(object layerKey, Vector2 scale)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetScale(layer, scale);
        }


        public void LayerSetRotation(int layer, Angle rotation)
        {
            if (!TryGetLayer(layer, out var theLayer, true))
                return;
            theLayer.Rotation = rotation;
            RebuildBounds();
        }

        public void LayerSetRotation(object layerKey, Angle rotation)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetRotation(layer, rotation);
        }

        public void LayerSetVisible(int layer, bool visible)
        {
            if (!TryGetLayer(layer, out var theLayer, true))
                return;

            theLayer.Visible = visible;
        }

        public void LayerSetVisible(object layerKey, bool visible)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetVisible(layer, visible);
        }

        public void LayerSetColor(int layer, Color color)
        {
            if (!TryGetLayer(layer, out var theLayer, true))
                return;

            theLayer.Color = color;

            RebuildBounds();
        }

        public void LayerSetColor(object layerKey, Color color)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetColor(layer, color);
        }

        public void LayerSetDirOffset(int layer, DirectionOffset offset)
        {
            if (!TryGetLayer(layer, out var theLayer, true))
                return;

            theLayer.DirOffset = offset;

            RebuildBounds();
        }

        public void LayerSetDirOffset(object layerKey, DirectionOffset offset)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetDirOffset(layer, offset);
        }

        public void LayerSetAnimationTime(int layer, float animationTime)
        {
            if (!TryGetLayer(layer, out var theLayer, true))
                return;

            theLayer.SetAnimationTime(animationTime);
        }

        public void LayerSetAnimationTime(object layerKey, float animationTime)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetAnimationTime(layer, animationTime);
        }

        public void LayerSetAutoAnimated(int layer, bool autoAnimated)
        {
            if (!TryGetLayer(layer, out var theLayer, true))
                return;

            theLayer.AutoAnimated = autoAnimated;
        }

        public void LayerSetAutoAnimated(object layerKey, bool autoAnimated)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetAutoAnimated(layer, autoAnimated);
        }

        public void LayerSetOffset(int layer, Vector2 layerOffset)
        {
            if (!TryGetLayer(layer, out var theLayer, true))
                return;

            theLayer.Offset = layerOffset;

            RebuildBounds();
        }

        public void LayerSetOffset(object layerKey, Vector2 layerOffset)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetOffset(layer, layerOffset);
        }

        public void LayerSetRenderingStrategy(int layer, LayerRenderingStrategy renderingStrategy)
        {
            if (!TryGetLayer(layer, out var theLayer, true))
                return;

            theLayer.RenderingStrategy = renderingStrategy;
        }

        public void LayerSetRenderingStrategy(object layerKey, LayerRenderingStrategy renderingStrategy)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetRenderingStrategy(layer, renderingStrategy);
        }

        /// <inheritdoc />
        public RSI.StateId LayerGetState(int layer)
        {
            if (!TryGetLayer(layer, out var theLayer, true))
                return default;

            return theLayer.State;
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
        public void Render(DrawingHandleWorld drawingHandle, Angle eyeRotation, Angle worldRotation, Direction? overrideDirection = null, Vector2 position = default)
        {
            RenderInternal(drawingHandle, eyeRotation, worldRotation, position, overrideDirection);
        }

        [DataField("noRot")] private bool _screenLock = false;

        /// <summary>
        /// If the sprite only has 1 direction should it snap at cardinals if rotated.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool SnapCardinals
        {
            get => _snapCardinals;
            set
            {
                if (value == _snapCardinals)
                    return;

                _snapCardinals = value;
                RebuildBounds();
            }
        }

        [DataField("snapCardinals")]
        private bool _snapCardinals = false;

        [DataField("overrideDir")]
        [ViewVariables(VVAccess.ReadWrite)]
        public Direction DirectionOverride = Direction.East;

        [DataField("enableOverrideDir")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool EnableDirectionOverride;

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        public bool NoRotation { get => _screenLock; set => _screenLock = value; }

        internal void RenderInternal(DrawingHandleWorld drawingHandle, Angle eyeRotation, Angle worldRotation, Vector2 worldPosition, Direction? overrideDirection)
        {
            var angle = worldRotation + eyeRotation; // angle on-screen. Used to decide the direction of 4/8 directional RSIs
            angle = angle.Reduced().FlipPositive();  // Reduce the angles to fix math shenanigans

            var cardinal = Angle.Zero;

            // If we have a 1-directional sprite then snap it to try and always face it south if applicable.
            if (!NoRotation && SnapCardinals)
            {
                cardinal = angle.GetCardinalDir().ToAngle();
            }

            // worldRotation + eyeRotation should be the angle of the entity on-screen. If no-rot is enabled this is just set to zero.
            // However, at some point later the eye-matrix is applied separately, so we subtract -eye rotation for now:
            var entityMatrix = Matrix3Helpers.CreateTransform(worldPosition, NoRotation ? -eyeRotation : worldRotation - cardinal);

            var transformSprite = Matrix3x2.Multiply(LocalMatrix, entityMatrix);

            if (GranularLayersRendering)
            {
                //Default rendering
                entityMatrix = Matrix3Helpers.CreateTransform(worldPosition, worldRotation);
                var transformDefault = Matrix3x2.Multiply(LocalMatrix, entityMatrix);
                //Snap to cardinals
                entityMatrix = Matrix3Helpers.CreateTransform(worldPosition, worldRotation - angle.GetCardinalDir().ToAngle());
                var transformSnap = Matrix3x2.Multiply(LocalMatrix, entityMatrix);
                //No rotation
                entityMatrix = Matrix3Helpers.CreateTransform(worldPosition, -eyeRotation);
                var transformNoRot = Matrix3x2.Multiply(LocalMatrix, entityMatrix);

                foreach (var layer in Layers) {
                    switch (layer.RenderingStrategy)
                    {
                        case LayerRenderingStrategy.NoRotation:
                            layer.Render(drawingHandle, ref transformNoRot, angle, overrideDirection);
                            break;
                        case LayerRenderingStrategy.SnapToCardinals:
                            layer.Render(drawingHandle, ref transformSnap, angle, overrideDirection);
                            break;
                        case LayerRenderingStrategy.Default:
                            layer.Render(drawingHandle, ref transformDefault, angle, overrideDirection);
                            break;
                        case LayerRenderingStrategy.UseSpriteStrategy:
                            layer.Render(drawingHandle, ref transformSprite, angle, overrideDirection);
                            break;
                        default:
                            Logger.Error($"Tried to render a layer with unknown rendering stragegy: {layer.RenderingStrategy}");
                            break;
                    }
                }
            }

            else
            {
                foreach (var layer in Layers)
                {
                    layer.Render(drawingHandle, ref transformSprite, angle, overrideDirection);
                }
            }
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

            return state.RsiDirections switch
            {
                RsiDirectionType.Dir1 => 1,
                RsiDirectionType.Dir4 => 4,
                RsiDirectionType.Dir8 => 8,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private void QueueUpdateRenderTree()
        {
            if (TreeUpdateQueued || !Owner.IsValid())
                return;

            // TODO whenever sprite comp gets ECS'd , just make this a direct method call.
            var ev = new QueueSpriteTreeUpdateEvent(entities.GetComponent<TransformComponent>(Owner));
            entities.EventBus.RaiseComponentEvent(Owner, this, ref ev);
        }

        private void QueueUpdateIsInert()
        {
            if (_inertUpdateQueued || !Owner.IsValid())
                return;

            // TODO whenever sprite comp gets ECS'd , just make this a direct method call.
            var ev = new SpriteUpdateInertEvent();
            entities.EventBus.RaiseComponentEvent(Owner, this, ref ev);
        }

        [Obsolete("Use SpriteSystem instead.")]
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
                Color, NoRotation, entities.GetComponent<TransformComponent>(Owner).WorldRotation.ToRsiDirection(RsiDirectionType.Dir8),
                DirectionOverride
            );

            foreach (var layer in Layers)
            {
                builder.AppendFormat(
                    "shad/tex/rsi/state/ant/anf/scl/rot/vis/col/dofs/renderstrat: {0}/{1}/{2}/{3}/{4}/{5}/{6}/{7}/{8}/{9}/{10}/{11}\n",
                    // These are references and don't include useful data for knowing where they came from, sadly.
                    // "is one set" is better than nothing at least.
                    layer.Shader != null, layer.Texture != null, layer.RSI != null,
                    layer.State,
                    layer.AnimationTimeLeft, layer.AnimationFrame, layer.Scale, layer.Rotation, layer.Visible,
                    layer.Color, layer.DirOffset, layer.RenderingStrategy
                );
            }

            return builder.ToString();
        }

        /// <inheritdoc/>
        public Box2Rotated CalculateRotatedBoundingBox(Vector2 worldPosition, Angle worldRotation, Angle eyeRot)
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

            // Next, what we do is take the box2 and apply the sprite's transform, and then the entity's transform. We
            // could do this via Matrix3.TransformBox, but that only yields bounding boxes. So instead we manually
            // transform our box by the combination of these matrices:

            Angle finalRotation = NoRotation
                ? Rotation - eyeRot
                : Rotation + worldRotation;

            // slightly faster path if offset == 0 (true for 99.9% of sprites)
            if (Offset == Vector2.Zero)
                return new Box2Rotated(Bounds.Translated(worldPosition), finalRotation, worldPosition);

            var adjustedOffset = NoRotation
                ? (-eyeRot).RotateVec(Offset)
                : worldRotation.RotateVec(Offset);

            Vector2 position = adjustedOffset + worldPosition;
            return new Box2Rotated(Bounds.Translated(position), finalRotation, position);
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

            [ViewVariables] public ProtoId<ShaderPrototype>? ShaderPrototype;
            [ViewVariables] public ShaderInstance? Shader;
            [ViewVariables] public Texture? Texture;

            /// <summary>
            /// If true, then this layer is drawn without lighting applied.
            /// Unshaded layers are given special treatment and don't just use the unshaded-shader to avoid having to
            /// unnecessarily swap out the light texture. This helps the number of batches that need to be sent to the
            /// GPU while drawing sprites.
            /// </summary>
            [ViewVariables] internal bool UnShaded;

            private RSI? _rsi;
            [ViewVariables] public RSI? RSI
            {
                get => _rsi;
                set
                {
                    if (_rsi == value)
                        return;

                    _rsi = value;
                    UpdateActualState();
                }
            }

            private RSI.StateId _state;
            [ViewVariables] public RSI.StateId State
            {
                get => _state;
                set
                {
                    if (_state == value)
                        return;

                    _state = value;
                    UpdateActualState();
                }
            }

            [ViewVariables] public float AnimationTimeLeft;
            [ViewVariables] public float AnimationTime;
            [ViewVariables] public int AnimationFrame;

            /// <summary>
            /// Is the animation currently playing in reverse.
            /// </summary>
            [ViewVariables] public bool Reversed { get; internal set; }

            /// <summary>
            /// If every animation delay finishes do we reverse it.
            /// </summary>
            /// <remarks>
            /// Only applies if the state is auto-animated.
            /// </remarks>
            [ViewVariables] public bool Cycle;

            private RSI.State? _actualState;
            [ViewVariables] public RSI.State? ActualState => _actualState;

            public Matrix3x2 LocalMatrix = Matrix3x2.Identity;

            [ViewVariables(VVAccess.ReadWrite)]
            public Vector2 Scale
            {
                get => _scale;
                set
                {
                    if (_scale.EqualsApprox(value)) return;

                    if (MathF.Abs(value.X) < 0.005f || MathF.Abs(value.Y) < 0.005f)
                    {
                        // Scales of ~0.0025 or lower can lead to singular matrices due to rounding errors.
                        Logger.Error($"Attempted to set layer sprite scale to very small values. Entity: {_parent.entities.ToPrettyString(_parent.Owner)}. Scale: {value}");
                        return;
                    }

                    _scale = value;
                    UpdateLocalMatrix();
                    _parent.RebuildBounds();
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
                    _parent.RebuildBounds();
                }
            }
            internal Angle _rotation = Angle.Zero;

            private bool _visible = true;
            [ViewVariables(VVAccess.ReadWrite)]
            public bool Visible
            {
                get => _visible;
                set
                {
                    if (_visible == value)
                        return;
                    _visible = value;

                    _parent.QueueUpdateIsInert();
                    _parent.RebuildBounds();
                }
            }

            [ViewVariables]
            public bool Blank => !State.IsValid && Texture == null;

            [ViewVariables(VVAccess.ReadWrite)]
            public Color Color { get; set; } = Color.White;

            private bool _autoAnimated = true;
            [ViewVariables(VVAccess.ReadWrite)]
            public bool AutoAnimated
            {
                get => _autoAnimated;
                set
                {
                    if (_autoAnimated == value)
                        return;
                    _autoAnimated = value;
                    _parent.QueueUpdateIsInert();
                }
            }

            [ViewVariables(VVAccess.ReadWrite)]
            public Vector2 Offset
            {
                get => _offset;
                set
                {
                    if (_offset.EqualsApprox(value)) return;

                    _offset = value;
                    UpdateLocalMatrix();
                    _parent.RebuildBounds();
                }
            }

            internal Vector2 _offset;

            [ViewVariables]
            public DirectionOffset DirOffset { get; set; }

            [ViewVariables]
            public RSI? ActualRsi => RSI ?? _parent.BaseRSI;

            /// <summary>
            ///    Whether the current layer have a specific rendering method (e.g no rotation or snap to cardinal)
            ///    The sprite GranularLayersRendering var must be set to true for this to have any effect.
            /// </summary>
            [ViewVariables]
            public LayerRenderingStrategy RenderingStrategy = LayerRenderingStrategy.UseSpriteStrategy;

            [ViewVariables(VVAccess.ReadWrite)]
            public CopyToShaderParameters? CopyToShaderParameters;

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
                    UnShaded = toClone.UnShaded;
                    ShaderPrototype = toClone.ShaderPrototype;
                }
                Texture = toClone.Texture;
                RSI = toClone.RSI;
                State = toClone.State;
                AnimationTimeLeft = toClone.AnimationTimeLeft;
                AnimationTime = toClone.AnimationTime;
                AnimationFrame = toClone.AnimationFrame;
                _scale = toClone.Scale;
                _rotation = toClone.Rotation;
                _offset = toClone.Offset;
                UpdateLocalMatrix();
                _visible = toClone._visible;
                Color = toClone.Color;
                DirOffset = toClone.DirOffset;
                _autoAnimated = toClone._autoAnimated;
                RenderingStrategy = toClone.RenderingStrategy;
                if (toClone.CopyToShaderParameters is { } copyToShaderParameters)
                    CopyToShaderParameters = new CopyToShaderParameters(copyToShaderParameters);
            }

            void ISerializationHooks.AfterDeserialization()
            {
                UpdateLocalMatrix();
            }

            internal void UpdateLocalMatrix()
            {
                LocalMatrix = Matrix3Helpers.CreateTransform(in _offset, in _rotation, in _scale);
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
                    Shader = ShaderPrototype,
                    State = State.Name,
                    Visible = Visible,
                    RsiPath = RSI?.Path.CanonPath,
                    RenderingStrategy = RenderingStrategy,
                    //todo TexturePath = Textur
                    //todo MapKeys
                };
            }

            float ISpriteLayer.AnimationTime
            {
                get => AnimationTime;
                set => SetAnimationTime(value);
            }

            int ISpriteLayer.AnimationFrame => AnimationFrame;

            public RsiDirection EffectiveDirection(Angle worldRotation)
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

            public RsiDirection EffectiveDirection(RSI.State state, Angle worldRotation,
                Direction? overrideDirection)
            {
                if (state.RsiDirections == RsiDirectionType.Dir1)
                {
                    return RsiDirection.South;
                }
                else
                {
                    RsiDirection dir;
                    if (overrideDirection != null)
                    {
                        dir = overrideDirection.Value.Convert(state.RsiDirections);
                    }
                    else
                    {
                        dir = worldRotation.ToRsiDirection(state.RsiDirections);
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
                AdvanceFrameAnimation(state);
            }

            public void SetAutoAnimated(bool value)
            {
                AutoAnimated = value;

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
                        Logger.ErrorS(LogCategory, "State '{0}' does not exist in set RSI ({1}). Trace:\n{2}", State, rsi?.Path.CanonPath ?? "null",
                            Environment.StackTrace);
                        Texture = null;
                    }
                }

                _parent.QueueUpdateRenderTree();
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

                _parent.QueueUpdateRenderTree();
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
            public Box2 CalculateBoundingBox()
            {
                var textureSize = (Vector2) PixelSize / EyeManager.PixelsPerMeter;
                var longestSide = MathF.Max(textureSize.X, textureSize.Y);
                var longestRotatedSide = Math.Max(longestSide, (textureSize.X + textureSize.Y) / MathF.Sqrt(2));

                Vector2 size;

                // If this layer has any form of arbitrary rotation, return a bounding box big enough to cover
                // any possible rotation.
                if (_rotation != 0)
                {
                    size = new Vector2(longestRotatedSide, longestRotatedSide);
                }
                else if (_parent.SnapCardinals && (!_parent.GranularLayersRendering || RenderingStrategy == LayerRenderingStrategy.UseSpriteStrategy)
                         || _parent.GranularLayersRendering && RenderingStrategy == LayerRenderingStrategy.SnapToCardinals)
                {
                    DebugTools.Assert(_actualState == null || _actualState.RsiDirections == RsiDirectionType.Dir1);
                    size = new Vector2(longestSide, longestSide);
                }
                else
                {
                    // Build the bounding box based on how many directions the sprite has
                    size = (_actualState?.RsiDirections) switch
                    {
                        // If we have four cardinal directions, take the longest side of our texture and square it, then turn that into our bounding box.
                        // This accounts for all possible rotations.
                        RsiDirectionType.Dir4 => new Vector2(longestSide, longestSide),

                        // If we have eight directions, find the maximum length of the texture (accounting for rotation), then square it to make
                        RsiDirectionType.Dir8 => new Vector2(longestRotatedSide, longestRotatedSide),

                        // If we have only one direction or an invalid RSI state, create a simple bounding box with the size of the texture.
                        _ => textureSize
                    };
                }

                return Box2.CenteredAround(Offset, size * _scale);
            }

            /// <summary>
            ///     Update Cached RSI state. State is cached to avoid calling this every time an entity gets drawn.
            /// </summary>
            internal void UpdateActualState()
            {
                _parent.QueueUpdateIsInert();
                if (!State.IsValid)
                {
                    _actualState = null;
                    return;
                }

                // Pull texture from RSI state
                var rsi = RSI ?? _parent.BaseRSI;
                if (rsi == null || !rsi.TryGetState(State, out _actualState))
                {
                    _actualState = GetFallbackState(_parent.resourceCache);
                }
            }

            /// <summary>
            ///     Given the apparent rotation of an entity on screen (world + eye rotation), get layer's matrix for drawing &
            ///     relevant RSI direction.
            /// </summary>
            public void GetLayerDrawMatrix(RsiDirection dir, out Matrix3x2 layerDrawMatrix)
            {
                if (_parent.NoRotation || dir == RsiDirection.South)
                    layerDrawMatrix = LocalMatrix;
                else
                {
                    layerDrawMatrix = Matrix3x2.Multiply(_rsiDirectionMatrices[(int)dir], LocalMatrix);
                }
            }

            private static Matrix3x2[] _rsiDirectionMatrices = new Matrix3x2[]
            {
                // array order chosen such that this array can be indexed by casing an RSI direction to an int
                Matrix3x2.Identity, // should probably just avoid matrix multiplication altogether if the direction is south.
                Matrix3Helpers.CreateRotation(-Direction.North.ToAngle()),
                Matrix3Helpers.CreateRotation(-Direction.East.ToAngle()),
                Matrix3Helpers.CreateRotation(-Direction.West.ToAngle()),
                Matrix3Helpers.CreateRotation(-Direction.SouthEast.ToAngle()),
                Matrix3Helpers.CreateRotation(-Direction.SouthWest.ToAngle()),
                Matrix3Helpers.CreateRotation(-Direction.NorthEast.ToAngle()),
                Matrix3Helpers.CreateRotation(-Direction.NorthWest.ToAngle())
            };

            /// <summary>
            ///     Converts an angle (between 0 and 2pi) to an RSI direction. This will slightly bias the angle to avoid flickering for
            ///     4-directional sprites.
            /// </summary>
            public static RsiDirection GetDirection(RsiDirectionType dirType, Angle angle)
            {
                if (dirType == RsiDirectionType.Dir1)
                    return RsiDirection.South;
                else if (dirType == RsiDirectionType.Dir8)
                    return angle.GetDir().Convert(dirType);

                // For 4-directional sprites, as entities are often moving & facing diagonally, we will slightly bias the
                // angle to avoid the sprite flickering.

                // mod is -0.5 for angles between 0-90 and 180-270, and +0.5 for 90-180 and 270-360
                var mod = (Math.Floor(angle.Theta / MathHelper.PiOver2) % 2) - 0.5;

                var modTheta = angle.Theta + mod * DirectionBias;

                return ((int)Math.Round(modTheta / MathHelper.PiOver2) % 4) switch
                {
                    0 => RsiDirection.South,
                    1 => RsiDirection.East,
                    2 => RsiDirection.North,
                    _ => RsiDirection.West,
                };
            }

            /// <summary>
            ///     Render a layer. This assumes that the input angle is between 0 and 2pi.
            /// </summary>
            internal void Render(DrawingHandleWorld drawingHandle, ref Matrix3x2 spriteMatrix, Angle angle, Direction? overrideDirection)
            {
                if (!Visible || Blank)
                    return;

                var dir = _actualState == null ? RsiDirection.South : GetDirection(_actualState.RsiDirections, angle);

                // Set the drawing transform for this layer
                GetLayerDrawMatrix(dir, out var layerMatrix);

                // The direction used to draw the sprite can differ from the one that the angle would naively suggest,
                // due to direction overrides or offsets.
                if (overrideDirection != null && _actualState != null)
                    dir = overrideDirection.Value.Convert(_actualState.RsiDirections);
                dir = dir.OffsetRsiDir(DirOffset);

                // Get the correct directional texture from the state, and draw it!
                var texture = GetRenderTexture(_actualState, dir);

                if (CopyToShaderParameters == null)
                {
                    // Set the drawing transform for this layer
                    var transformMatrix = Matrix3x2.Multiply(layerMatrix, spriteMatrix);
                    drawingHandle.SetTransform(in transformMatrix);

                    RenderTexture(drawingHandle, texture);
                }
                else
                {
                    // Multiple atrocities to god being committed right here.
                    var otherLayerIdx = _parent.LayerMap[CopyToShaderParameters.LayerKey!];
                    var otherLayer = _parent.Layers[otherLayerIdx];
                    if (otherLayer.Shader is not { } shader)
                    {
                        // No shader set apparently..?
                        return;
                    }

                    if (!shader.Mutable)
                        otherLayer.Shader = shader = shader.Duplicate();

                    var clydeTexture = Clyde.RenderHandle.ExtractTexture(texture, null, out var csr);
                    var sr = Clyde.RenderHandle.WorldTextureBoundsToUV(clydeTexture, csr);

                    if (CopyToShaderParameters.ParameterTexture is { } paramTexture)
                        shader.SetParameter(paramTexture, clydeTexture);

                    if (CopyToShaderParameters.ParameterUV is { } paramUV)
                    {
                        var uv = new Vector4(sr.Left, sr.Bottom, sr.Right, sr.Top);
                        shader.SetParameter(paramUV, uv);
                    }
                }
            }

            private void RenderTexture(DrawingHandleWorld drawingHandle, Texture texture)
            {
                if (Shader != null)
                    drawingHandle.UseShader(Shader);

                var layerColor = _parent.color * Color;

                DebugTools.Assert(layerColor is {R: >= 0, G: >= 0, B: >= 0, A: >= 0}, "Negative colour modulation");

                if (UnShaded)
                {
                    DebugTools.AssertNull(Shader);

                    // Negative modulation values are used to disable light shading in the default shader.
                    // Specifically we set colour = -1 - colour
                    // This ensures that non-negative values become negative & is trivially invertible.
                    // Alternatively we could just clamp the colour to [0,1] and subtract a constant.
                    layerColor = new(new SysVec4(-1) - layerColor.RGBA);
                }

                var textureSize = texture.Size / (float)EyeManager.PixelsPerMeter;
                var quad = Box2.FromDimensions(textureSize/-2, textureSize);

                drawingHandle.DrawTextureRectRegion(texture, quad, layerColor);

                if (Shader != null)
                    drawingHandle.UseShader(null);
            }

            private Texture GetRenderTexture(RSI.State? state, RsiDirection dir)
            {
                if (state == null)
                    return Texture ?? _parent.resourceCache.GetFallback<TextureResource>().Texture;

                return state.GetFrame(dir, AnimationFrame);
            }

            internal void AdvanceFrameAnimation(RSI.State state)
            {
                // Can't advance frames without more than 1 delay which is already checked above.
                var delayCount = state.DelayCount;
                while (AnimationTimeLeft < 0)
                {
                    if (Reversed)
                    {
                        AnimationFrame -= 1;

                        // Animation finished, do we cycle back to positive or reset.
                        if (AnimationFrame < 0)
                        {
                            if (Cycle)
                            {
                                AnimationFrame = 1;
                                Reversed = false;
                            }
                            else
                            {
                                AnimationFrame = delayCount - 1;
                            }

                            AnimationTime = -AnimationTimeLeft;
                        }
                    }
                    else
                    {
                        AnimationFrame += 1;

                        // Animation finished, do we reverse or reset.
                        if (AnimationFrame >= delayCount)
                        {
                            if (Cycle)
                            {
                                AnimationFrame = delayCount - 2;
                                Reversed = true;
                            }
                            else
                            {
                                AnimationFrame = 0;
                            }

                            AnimationTime = -AnimationTimeLeft;
                        }
                    }

                    AnimationTimeLeft += state.GetDelay(AnimationFrame);
                }
            }
        }

        /// <summary>
        /// Instantiated version of <see cref="PrototypeCopyToShaderParameters"/>.
        /// Has <see cref="LayerKey"/> actually resolved to a a real key.
        /// </summary>
        public sealed class CopyToShaderParameters(object layerKey)
        {
            public object LayerKey = layerKey;
            public string? ParameterTexture;
            public string? ParameterUV;

            public CopyToShaderParameters(CopyToShaderParameters toClone) : this(toClone.LayerKey)
            {
                ParameterTexture = toClone.ParameterTexture;
                ParameterUV = toClone.ParameterUV;
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

            // TODO when moving to a non-static method in a system, pass in IComponentFactory
            if (prototype.TryGetComponent(out IconComponent? icon))
            {
                var sys = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SpriteSystem>();
                results.Add(sys.GetIcon(icon));
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
            EntitySystem.Get<AppearanceSystem>().OnChangeData(dummy, spriteComponent);

            foreach (var layer in spriteComponent.AllLayers)
            {
                if (!layer.Visible) continue;

                if (layer.Texture != null)
                {
                    results.Add(layer.Texture);
                    continue;
                }

                if (!layer.RsiState.IsValid) continue;

                var rsi = layer.Rsi ?? spriteComponent.BaseRSI;
                if (rsi == null ||
                    !rsi.TryGetState(layer.RsiState, out var state))
                    continue;

                results.Add(state);
            }

            noRot = spriteComponent.NoRotation;

            entityManager.DeleteEntity(dummy);

            if (results.Count == 0)
                results.Add(resourceCache.GetFallback<TextureResource>().Texture);

            return results;
        }

        [Obsolete("Use SpriteSystem")]
        public static IRsiStateLike GetPrototypeIcon(EntityPrototype prototype, IResourceCache resourceCache)
        {
            // TODO when moving to a non-static method in a system, pass in IComponentFactory
            if (prototype.TryGetComponent(out IconComponent? icon))
            {
                var sys = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SpriteSystem>();
                return sys.GetIcon(icon);
            }

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


    [ByRefEvent]
    internal struct SpriteUpdateInertEvent
    {
    }
}
