using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using Robust.Client.ComponentTrees;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Clyde;
using Robust.Client.ResourceManagement;
using Robust.Client.Utility;
using Robust.Shared;
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
using DrawDepthTag = Robust.Shared.GameObjects.DrawDepth;
using static Robust.Shared.Serialization.TypeSerializers.Implementations.SpriteSpecifierSerializer;
using Direction = Robust.Shared.Maths.Direction;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Robust.Client.GameObjects
{
    [RegisterComponent]
    public sealed partial class SpriteComponent : Component, IComponentDebug, ISerializationHooks, IComponentTreeEntry<SpriteComponent>, IAnimationProperties
    {
        public const string LogCategory = "go.comp.sprite";

        [Dependency] private readonly IResourceCache resourceCache = default!;
        [Dependency] private readonly IPrototypeManager prototypes = default!;
        [Dependency] private readonly EntityManager entities = default!;
        [Dependency] private readonly IReflectionManager reflection = default!;

        /// <summary>
        ///     See <see cref="CVars.RenderSpriteDirectionBias"/>.
        /// </summary>
        public static double DirectionBias = -0.05;

        /// <summary>
        ///     Whether the layers have independant drawing strategies, e.g some may snap to cardinals while others won't.
        ///     The sprite should still set its global rendering method (e.g NoRot or SnapCardinals), this only gives finer control over how layers are rendered internally.
        /// </summary>
        [DataField] // TODO Sprite access restrict.
        public bool GranularLayersRendering = false;

        [DataField("visible")]
        internal bool _visible = true;

        // VV convenience variable to examine layer objects using layer keys
        // ReSharper disable once UnusedMember.Local
        [ViewVariables]
        private Dictionary<object, Layer> MappedLayers => LayerMap.ToDictionary(x => x.Key, x => Layers[x.Value]);

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Visible
        {
            get => _visible;
            [Obsolete("Use SpriteSystem.SetVisible() instead.")]
            set => Sys.SetVisible((Owner, this), value);
        }

        private SpriteSystem? _sys;
        private SpriteTreeSystem? _treeSys;
        private SpriteSystem Sys => _sys ??= (entities.Started ? entities.System<SpriteSystem>() : null)!;
        private SpriteTreeSystem TreeSys => _treeSys ??= (entities.Started ? entities.System<SpriteTreeSystem>() : null)!;

        [DataField("drawdepth", customTypeSerializer: typeof(ConstantSerializer<DrawDepthTag>))]
        internal int drawDepth = DrawDepthTag.Default;

        /// <summary>
        ///     Z-index for drawing.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int DrawDepth
        {
            get => drawDepth;
            [Obsolete("Use SpriteSystem.SetDrawDepth() instead.")]
            set => Sys.SetDrawDepth((Owner, this), value);
        }

        [DataField("scale")] // Explicit name, in case this field ever gets renamed
        internal Vector2 scale = Vector2.One;

        /// <summary>
        ///     A scale applied to all layers.
        /// </summary>
        [Animatable]
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 Scale
        {
            get => scale;
            [Obsolete("Use SpriteSystem.SetScale() instead.")]
            set => Sys.SetScale((Owner, this), value);
        }

        [DataField("rotation")] // Explicit name, in case this field ever gets renamed
        internal Angle rotation = Angle.Zero;

        [Animatable]
        [ViewVariables(VVAccess.ReadWrite)]
        public Angle Rotation
        {
            get => rotation;
            [Obsolete("Use SpriteSystem.SetRotation() instead.")]
            set => Sys.SetRotation((Owner, this), value);
        }

        [DataField("offset")] // Explicit name, in case this field ever gets renamed
        internal Vector2 offset = Vector2.Zero;

        /// <summary>
        ///     Offset applied to all layers.
        /// </summary>
        [Animatable]
        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 Offset
        {
            get => offset;
            [Obsolete("Use SpriteSystem.SetOffset() instead.")]
            set => Sys.SetOffset((Owner, this), value);
        }

        [DataField("color")] // Explicit name, in case this field ever gets renamed
        internal Color color = Color.White;

        [Animatable]
        [ViewVariables(VVAccess.ReadWrite)]
        public Color Color
        {
            get => color;
            [Obsolete("Use SpriteSystem.SetColor() instead.")]
            set => Sys.SetColor((Owner, this), value);
        }

        public Matrix3x2 LocalMatrix = Matrix3x2.Identity;

        [ViewVariables]
        public DynamicTree<ComponentTreeEntry<SpriteComponent>>? Tree { get; set; }

        public EntityUid? TreeUid { get; set; }

        public bool AddToTree => Visible && !ContainerOccluded && Layers.Count > 0;

        public bool TreeUpdateQueued { get; set; }

        internal RSI? _baseRsi;

        [ViewVariables(VVAccess.ReadWrite)]
        public RSI? BaseRSI
        {
            get => _baseRsi;
            [Obsolete("Use SpriteSystem.SetBaseRSI() instead.")]
            set => Sys.SetBaseRsi((Owner, this), value);
        }

        [DataField("sprite", readOnly: true)] private string? rsi;
        [DataField("layers", readOnly: true)] private List<PrototypeLayerData> layerDatums = new();

        [DataField(readOnly: true)] private string? state;
        [DataField(readOnly: true)] private string? texture;

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
            [Obsolete("Use SpriteSystem.SetContainerOccluded() instead.")]
            set => Sys.SetContainerOccluded((Owner, this), value);
        }

        internal bool _containerOccluded;

        /// <summary>
        /// Whether or not the sprite's local bounding box is dirty and need to be rebuilt.
        /// </summary>
        internal bool BoundsDirty = true;

        internal Box2 _bounds;

        [Obsolete("Use SpriteSystem.GetLocalBounds() instead.")]
        public Box2 Bounds => Sys.GetLocalBounds((Owner, this));

        [ViewVariables(VVAccess.ReadWrite)] internal bool _inertUpdateQueued;

        /// <summary>
        ///     Shader instance to use when drawing the final sprite to the world.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public ShaderInstance? PostShader
        {
            get;
            // This will get obsoleted, but I only want to mark it as obsolete when multi-shader support is added, so
            // that people can use the appropriate method and don't migrate to an incorrect new method that wont
            // be obsoleted.
            set;
        }

        /// <summary>
        ///     Whether to pass the screen texture to the <see cref="PostShader"/>.
        /// </summary>
        /// <remarks>
        ///     Should be false unless you really need it.
        /// </remarks>
        [DataField]
        public bool GetScreenTexture;

        /// <summary>
        ///     If true, this raise a entity system event before rendering this sprite, allowing systems to modify the
        ///     shader parameters. Usually this can just be done via a frame-update, but some shaders require
        ///     information about the viewport / eye.
        /// </summary>
        [DataField]
        public bool RaiseShaderEvent;

        [ViewVariables] internal Dictionary<object, int> LayerMap { get; set; } = new();
        [ViewVariables] internal List<Layer> Layers = new();

        [ViewVariables(VVAccess.ReadWrite)] public uint RenderOrder { get; set; }

        [ViewVariables(VVAccess.ReadWrite)] public bool IsInert { get; internal set; }

        public ISpriteLayer this[int layer] => Layers[layer];
        public ISpriteLayer this[Index layer] => Layers[layer];
        public ISpriteLayer this[object layerKey] => this[LayerMap[layerKey]];
        public IEnumerable<ISpriteLayer> AllLayers => Layers;

        void ISerializationHooks.AfterDeserialization()
        {
            // Please somebody burn this to the ground. There is so much spaghetti.
            // Why has no one answered my prayers.

            IoCManager.InjectDependencies(this);
            if (!string.IsNullOrWhiteSpace(rsi))
            {
                var rsiPath = TextureRoot / rsi;
                if (resourceCache.TryGetResource(rsiPath, out RSIResource? resource))
                    _baseRsi = resource.RSI;
                else
                    Logger.ErrorS(LogCategory, "Unable to load RSI '{0}'.", rsiPath);
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
                    var layer = new Layer((Owner, this), Layers.Count);
                    Layers.Add(layer);
                    LayerSetData(layer, datum);
                }

            }

            BoundsDirty = true;
            LocalMatrix = Matrix3Helpers.CreateTransform(in offset, in rotation, in scale);
        }

        /// <summary>
        /// Update this sprite component to visibly match the current state of other at the time
        /// this is called. Does not keep them perpetually in sync.
        /// This does some deep copying thus exerts some gc pressure, so avoid this for hot code paths.
        /// </summary>
        [Obsolete("Use SpriteSystem.CopySprite() instead.")]
        public void CopyFrom(SpriteComponent other)
        {
            Sys.CopySprite((other.Owner, other), (Owner, this));
        }

        [Obsolete("Use LocalMatrix")]
        public Matrix3x2 GetLocalMatrix()
        {
            return LocalMatrix;
        }

        [Obsolete("Use SpriteSystem.LayerMapSet() instead.")]
        public void LayerMapSet(object key, int layer)
        {
            if (layer < 0 || layer >= Layers.Count)
            {
                throw new ArgumentOutOfRangeException();
            }

            LayerMap.Add(key, layer);
        }

        [Obsolete("Use SpriteSystem.LayerMapRemove() instead.")]
        public void LayerMapRemove(object key)
        {
            LayerMap.Remove(key);
        }

        [Obsolete("Use SpriteSystem.LayerMapGet() instead.")]
        public int LayerMapGet(object key)
        {
            return LayerMap[key];
        }

        [Obsolete("Use SpriteSystem.LayerMapTryGet() instead.")]
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

        [Obsolete("Use SpriteSystem.TryGetLayer() instead.")]
        public bool TryGetLayer(int index, [NotNullWhen(true)] out Layer? layer, bool logError = false)
            => Sys.TryGetLayer((Owner, this), index, out layer, logError);

        [Obsolete("Use SpriteSystem.LayerExists() instead.")]
        public bool LayerExists(int layer, bool logError = true)
            => Sys.LayerExists((Owner, this), layer);

        [Obsolete("Use SpriteSystem.LayerExists() instead.")]
        public bool LayerExists(object key, bool logError = false) => LayerMapTryGet(key, out _, logError);

        [Obsolete("Use SpriteSystem.LayerMapReserve() instead.")]
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

        [Obsolete("Use SpriteSystem.AddBlankLayer() instead.")]
        public int AddBlankLayer(int? newIndex = null)
            => Sys.AddBlankLayer((Owner, this), newIndex).Index;

        [Obsolete("Use SpriteSystem.AddLayer() instead.")]
        public int AddLayer(PrototypeLayerData layerDatum, int? newIndex = null)
            => Sys.AddLayer((Owner, this), layerDatum, newIndex);

        [Obsolete("Use SpriteSystem.AddTextureLayer() instead.")]
        public int AddLayer(string texturePath, int? newIndex = null)
        {
            return AddLayer(new ResPath(texturePath), newIndex);
        }

        [Obsolete("Use SpriteSystem.AddTextureLayer() instead.")]
        public int AddLayer(ResPath texturePath, int? newIndex = null)
            => Sys.AddTextureLayer((Owner, this), texturePath, newIndex);

        [Obsolete("Use SpriteSystem.AddTextureLayer() instead.")]
        public int AddLayer(Texture? texture, int? newIndex = null)
            => Sys.AddTextureLayer((Owner, this), texture, newIndex);

        [Obsolete("Use SpriteSystem.AddRsiLayer() instead.")]
        public int AddLayer(RSI.StateId stateId, int? newIndex = null)
            => Sys.AddRsiLayer((Owner, this), stateId, null, newIndex);

        [Obsolete("Use SpriteSystem.AddRsiLayer() instead.")]
        public int AddLayerState(string stateId, int? newIndex = null)
        {
            return AddLayer(new RSI.StateId(stateId), newIndex);
        }

        [Obsolete("Use SpriteSystem.AddRsiLayer() instead.")]
        public int AddLayer(RSI.StateId stateId, string rsiPath, int? newIndex = null)
        {
            return AddLayer(stateId, new ResPath(rsiPath), newIndex);
        }

        [Obsolete("Use SpriteSystem.AddRsiLayer() instead.")]
        public int AddLayerState(string stateId, string rsiPath, int? newIndex = null)
        {
            return AddLayer(new RSI.StateId(stateId), rsiPath, newIndex);
        }

        [Obsolete("Use SpriteSystem.AddRsiLayer() instead.")]
        public int AddLayer(RSI.StateId stateId, ResPath rsiPath, int? newIndex = null)
            => Sys.AddRsiLayer((Owner, this), stateId, rsiPath, newIndex);

        [Obsolete("Use SpriteSystem.AddRsiLayer() instead.")]
        public int AddLayerState(string stateId, ResPath rsiPath, int? newIndex = null)
        {
            return AddLayer(new RSI.StateId(stateId), rsiPath, newIndex);
        }

        [Obsolete("Use SpriteSystem.AddRsiLayer() instead.")]
        public int AddLayer(RSI.StateId stateId, RSI? rsi, int? newIndex = null)
            => Sys.AddRsiLayer((Owner, this), stateId, rsi, newIndex);

        [Obsolete("Use SpriteSystem.AddRsiLayer() instead.")]
        public int AddLayerState(string stateId, RSI rsi, int? newIndex = null)
        {
            return AddLayer(new RSI.StateId(stateId), rsi, newIndex);
        }

        [Obsolete("Use SpriteSystem.AddLayer() instead.")]
        public int AddLayer(SpriteSpecifier specifier, int? newIndex = null)
            => Sys.AddLayer((Owner, this), specifier, newIndex);

        [Obsolete("Use SpriteSystem.RemoveLayer() instead.")]
        public void RemoveLayer(int layer)
            => Sys.RemoveLayer((Owner, this), layer);

        [Obsolete("Use SpriteSystem.RemoveLayer() instead.")]
        public void RemoveLayer(object layerKey)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            RemoveLayer(layer);
        }

        [DataField("snapCardinals")]
        internal bool _snapCardinals = false;

        /// <summary>
        /// If the sprite only has 1 direction should it snap at cardinals if rotated.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool SnapCardinals
        {
            get => _snapCardinals;
            [Obsolete("Use SpriteSystem.SnapCardinals() instead.")]
            set => Sys.SetSnapCardinals((Owner, this), value);
        }

        /// <summary>
        /// If true, the sprite will always be rendered as if its world rotation relative to the screen's eye is 0.
        /// Note for 4- or 8- directional sprites, the relative rotation is still used to choose the RSI state.
        /// </summary>
        /// <remarks>
        /// E.g., this is used to ensure that players/mobs are always standing upright, but the sprite will still change
        /// based on the direction that a mob is looking in.
        /// </remarks>
        [DataField("noRot")]
        public bool NoRotation;

        // TODO SPRITE
        // When refactoring, make this nullable and remove EnableDirectionOverride
        [DataField("overrideDir")]
        public Direction DirectionOverride = Direction.East;

        [DataField("enableOverrideDir")]
        public bool EnableDirectionOverride;

        [Obsolete("Use SpriteSystem.LayerSetData() instead.")]
        public void LayerSetData(int index, PrototypeLayerData layerDatum)
            => Sys.LayerSetData((Owner, this), index, layerDatum);

        [Obsolete("Use SpriteSystem.LayerSetData() instead.")]
        internal void LayerSetData(Layer layer, PrototypeLayerData layerDatum)
        {
            DebugTools.AssertEqual(layer, layer.Owner.Comp.Layers[layer.Index]);

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
                        if (mappedIndex != layer.Index)
                            Logger.ErrorS(LogCategory, "Duplicate layer map key definition: {0}", key);
                        continue;
                    }

                    LayerMap[key] = layer.Index;
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

            BoundsDirty = true;
            layer.BoundsDirty = true;
            // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            if (Owner != EntityUid.Invalid)
                TreeSys?.QueueTreeUpdate((Owner, this));
        }

        private object ParseKey(string keyString)
        {
            if (reflection.TryParseEnumReference(keyString, out var @enum))
                return @enum;

            return keyString;
        }

        [Obsolete("Use SpriteSystem.LayerSetData() instead.")]
        public void LayerSetData(object layerKey, PrototypeLayerData data)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetData(layer, data);
        }

        [Obsolete("Use SpriteSystem.LayerSetSprite() instead.")]
        public void LayerSetSprite(int layer, SpriteSpecifier specifier)
            => Sys.LayerSetSprite((Owner, this), layer, specifier);

        [Obsolete("Use SpriteSystem.LayerSetSprite() instead.")]
        public void LayerSetSprite(object layerKey, SpriteSpecifier specifier)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetSprite(layer, specifier);
        }

        [Obsolete("Use SpriteSystem.LayerSetTexture() instead.")]
        public void LayerSetTexture(int layer, Texture? texture)
            => Sys.LayerSetTexture((Owner, this), layer, texture);

        [Obsolete("Use SpriteSystem.LayerSetTexture() instead.")]
        public void LayerSetTexture(object layerKey, Texture texture)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetTexture(layer, texture);
        }

        [Obsolete("Use SpriteSystem.LayerSetTexture() instead.")]
        public void LayerSetTexture(int layer, string texturePath)
        {
            LayerSetTexture(layer, new ResPath(texturePath));
        }

        [Obsolete("Use SpriteSystem.LayerSetTexture() instead.")]
        public void LayerSetTexture(object layerKey, string texturePath)
        {
            LayerSetTexture(layerKey, new ResPath(texturePath));
        }

        [Obsolete("Use SpriteSystem.LayerSetTexture() instead.")]
        public void LayerSetTexture(int layer, ResPath texturePath)
            => Sys.LayerSetTexture((Owner, this), layer, texturePath);

        [Obsolete("Use SpriteSystem.LayerSetTexture() instead.")]
        public void LayerSetTexture(object layerKey, ResPath texturePath)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetTexture(layer, texturePath);
        }

        [Obsolete("Use SpriteSystem.LayerSetRsiState() instead.")]
        public void LayerSetState(int layer, RSI.StateId stateId)
            => Sys.LayerSetRsiState((Owner, this), layer, stateId);

        [Obsolete("Use SpriteSystem.LayerSetRsiState() instead.")]
        public void LayerSetState(object layerKey, RSI.StateId stateId)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetState(layer, stateId);
        }

        [Obsolete("Use SpriteSystem.LayerSetRsi() instead.")]
        public void LayerSetState(int layer, RSI.StateId stateId, RSI? rsi)
            => Sys.LayerSetRsi((Owner, this), layer, rsi, stateId);

        [Obsolete("Use SpriteSystem.LayerSetRsi() instead.")]
        public void LayerSetState(object layerKey, RSI.StateId stateId, RSI rsi)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetState(layer, stateId, rsi);
        }

        [Obsolete("Use SpriteSystem.LayerSetRsiState() instead.")]
        public void LayerSetState(int layer, RSI.StateId stateId, string rsiPath)
        {
            LayerSetState(layer, stateId, new ResPath(rsiPath));
        }

        [Obsolete("Use SpriteSystem.LayerSetRsi() instead.")]
        public void LayerSetState(object layerKey, RSI.StateId stateId, string rsiPath)
        {
            LayerSetState(layerKey, stateId, new ResPath(rsiPath));
        }

        [Obsolete("Use SpriteSystem.LayerSetRsi() instead.")]
        public void LayerSetState(int layer, RSI.StateId stateId, ResPath rsiPath)
            => Sys.LayerSetRsi((Owner, this), layer, rsiPath, stateId);

        [Obsolete("Use SpriteSystem.LayerSetRsi() instead.")]
        public void LayerSetState(object layerKey, RSI.StateId stateId, ResPath rsiPath)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetState(layer, stateId, rsiPath);
        }

        [Obsolete("Use SpriteSystem.LayerSetRsi() instead.")]
        public void LayerSetRSI(int layer, RSI? rsi)
            => Sys.LayerSetRsi((Owner, this), layer, rsi);

        [Obsolete("Use SpriteSystem.LayerSetRsi() instead.")]
        public void LayerSetRSI(object layerKey, RSI rsi)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetRSI(layer, rsi);
        }

        [Obsolete("Use SpriteSystem.LayerSetRsi() instead.")]
        public void LayerSetRSI(int layer, string rsiPath)
        {
            LayerSetRSI(layer, new ResPath(rsiPath));
        }

        [Obsolete("Use SpriteSystem.LayerSetRsi() instead.")]
        public void LayerSetRSI(object layerKey, string rsiPath)
        {
            LayerSetRSI(layerKey, new ResPath(rsiPath));
        }

        [Obsolete("Use SpriteSystem.LayerSetRsi() instead.")]
        public void LayerSetRSI(int layer, ResPath rsiPath)
            => Sys.LayerSetRsi((Owner, this), layer, rsiPath);

        [Obsolete("Use SpriteSystem.LayerSetRsi() instead.")]
        public void LayerSetRSI(object layerKey, ResPath rsiPath)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetRSI(layer, rsiPath);
        }

        [Obsolete("Use SpriteSystem.LayerSetScale() instead.")]
        public void LayerSetScale(int layer, Vector2 scale)
            => Sys.LayerSetScale((Owner, this), layer, scale);

        [Obsolete("Use SpriteSystem.LayerSetScale() instead.")]
        public void LayerSetScale(object layerKey, Vector2 scale)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetScale(layer, scale);
        }

        [Obsolete("Use SpriteSystem.LayerSetRotation() instead.")]
        public void LayerSetRotation(int layer, Angle rotation)
            => Sys.LayerSetRotation((Owner, this), layer, rotation);

        [Obsolete("Use SpriteSystem.LayerSetRotation() instead.")]
        public void LayerSetRotation(object layerKey, Angle rotation)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetRotation(layer, rotation);
        }

        [Obsolete("Use SpriteSystem.LayerSetOffset() instead.")]
        public void LayerSetOffset(int layer, Vector2 layerOffset)
            => Sys.LayerSetOffset((Owner, this), layer, layerOffset);

        [Obsolete("Use SpriteSystem.LayerSetOffset() instead.")]
        public void LayerSetOffset(object layerKey, Vector2 layerOffset)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetOffset(layer, layerOffset);
        }

        [Obsolete("Use SpriteSystem.LayerSetVisible() instead.")]
        public void LayerSetVisible(int layer, bool visible)
            => Sys.LayerSetVisible((Owner, this), layer, visible);

        [Obsolete("Use SpriteSystem.LayerSetVisible() instead.")]
        public void LayerSetVisible(object layerKey, bool visible)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetVisible(layer, visible);
        }

        [Obsolete("Use SpriteSystem.LayerSetColor() instead.")]
        public void LayerSetColor(int layer, Color color)
            => Sys.LayerSetColor((Owner, this), layer, color);

        [Obsolete("Use SpriteSystem.LayerSetColor() instead.")]
        public void LayerSetColor(object layerKey, Color color)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetColor(layer, color);
        }

        [Obsolete("Use SpriteSystem.LayerSetDirOffset() instead.")]
        public void LayerSetDirOffset(int layer, DirectionOffset offset)
            => Sys.LayerSetDirOffset((Owner, this), layer, offset);

        [Obsolete("Use SpriteSystem.LayerSetDirOffset() instead.")]
        public void LayerSetDirOffset(object layerKey, DirectionOffset offset)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetDirOffset(layer, offset);
        }

        [Obsolete("Use SpriteSystem.LayerSetAnimationTime() instead.")]
        public void LayerSetAnimationTime(int layer, float animationTime)
            => Sys.LayerSetAnimationTime((Owner, this), layer, animationTime);

        [Obsolete("Use SpriteSystem.LayerSetAnimationTime() instead.")]
        public void LayerSetAnimationTime(object layerKey, float animationTime)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetAnimationTime(layer, animationTime);
        }

        [Obsolete("Use SpriteSystem.LayerSetAutoAnimated() instead.")]
        public void LayerSetAutoAnimated(int layer, bool autoAnimated)
            => Sys.LayerSetAutoAnimated((Owner, this), layer, autoAnimated);

        [Obsolete("Use SpriteSystem.LayerSetAutoAnimated() instead.")]
        public void LayerSetAutoAnimated(object layerKey, bool autoAnimated)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetAutoAnimated(layer, autoAnimated);
        }

        [Obsolete("Use SpriteSystem.LayerSetRenderingStrategy() instead.")]
        public void LayerSetRenderingStrategy(int layer, LayerRenderingStrategy renderingStrategy)
            => Sys.LayerSetRenderingStrategy((Owner, this), layer, renderingStrategy);

        [Obsolete("Use SpriteSystem.LayerSetRenderingStrategy() instead.")]
        public void LayerSetRenderingStrategy(object layerKey, LayerRenderingStrategy renderingStrategy)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetRenderingStrategy(layer, renderingStrategy);
        }

        [Obsolete("Use SpriteSystem.LayerGetRsiState() instead.")]
        public RSI.StateId LayerGetState(int layer)
        {
            if (!TryGetLayer(layer, out var theLayer, true))
                return default;

            return theLayer.State;
        }

        [Obsolete("Use SpriteSystem.LayerGetEffectiveRsi() instead.")]
        public RSI? LayerGetActualRSI(int layer)
        {
            return this[layer].ActualRsi;
        }

        [Obsolete("Use SpriteSystem.LayerGetEffectiveRsi() instead.")]
        public RSI? LayerGetActualRSI(object layerKey)
        {
            return this[layerKey].ActualRsi;
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

        public void LayerSetShader(object layerKey, ShaderInstance shader, string? prototype = null)
        {
            if (!LayerMapTryGet(layerKey, out var layer, true))
                return;

            LayerSetShader(layer, shader, prototype);
        }

        public void LayerSetShader(int layer, string shaderName)
        {
            if (!prototypes.TryIndex<ShaderPrototype>(shaderName, out var prototype))
            {
                Logger.ErrorS(LogCategory,
                    "Shader prototype '{0}' does not exist. Trace:\n{1}",
                    shaderName,
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

        // Lobby SpriteView rendering path
        [Obsolete("Use SpriteSystem.Render() instead.")]
        public void Render(DrawingHandleWorld drawingHandle, Angle eyeRotation, Angle worldRotation, Direction? overrideDirection = null, Vector2 position = default)
        {
            Sys.RenderSprite((Owner, this), drawingHandle, eyeRotation, worldRotation, position, overrideDirection);
        }

        [Obsolete("Use SpriteSystem.LayerGetDirectionCount() instead.")]
        public int GetLayerDirectionCount(ISpriteLayer layer)
        {
            if (!layer.RsiState.IsValid)
                return 1;

            // Pull texture from RSI state instead.
            var rsi = layer.Rsi ?? BaseRSI;
            if (rsi == null || !rsi.TryGetState(layer.RsiState, out var state))
            {
                state = Sys.GetFallbackState();
            }

            return state.RsiDirections switch
            {
                RsiDirectionType.Dir1 => 1,
                RsiDirectionType.Dir4 => 4,
                RsiDirectionType.Dir8 => 8,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public string GetDebugString()
        {
            var builder = new StringBuilder();
            builder.Append(
                $"vis/depth/scl/rot/ofs/col/norot/override: {Visible}/{DrawDepth}/{Scale}/{Rotation}/{Offset}/{Color}/{NoRotation}/{DirectionOverride}/\n"
            );

            foreach (var layer in Layers)
            {
                builder.AppendFormat(
                    "shad/tex/rsi/state/ant/anf/scl/rot/vis/col/dofs/renderstrat: {0}/{1}/{2}/{3}/{4}/{5}/{6}/{7}/{8}/{9}/{10}/{11}\n",
                    // These are references and don't include useful data for knowing where they came from, sadly.
                    // "is one set" is better than nothing at least.
                    layer.Shader != null,
                    layer.Texture != null,
                    layer.RSI != null,
                    layer.State,
                    layer.AnimationTimeLeft,
                    layer.AnimationFrame,
                    layer.Scale,
                    layer.Rotation,
                    layer.Visible,
                    layer.Color,
                    layer.DirOffset,
                    layer.RenderingStrategy
                );
            }

            return builder.ToString();
        }

        [Obsolete("Use SpriteSystem.CalculateBounds() instead.")]
        public Box2Rotated CalculateRotatedBoundingBox(Vector2 worldPosition, Angle worldRotation, Angle eyeRot)
        {
            return Sys.CalculateBounds((Owner, this), worldPosition, worldRotation, eyeRot);
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
            internal SpriteComponent _parent => Owner.Comp;

            /// <summary>
            /// The entity that this layer belongs to.
            /// </summary>
            [Access(typeof(SpriteSystem), typeof(SpriteComponent))]
            [ViewVariables] internal Entity<SpriteComponent> Owner;
            // Internal, because I might want to change this in future W/O breaking changes.
            // Also, it's possible for SpriteComponent to be null if it is not currently attached to a sprite.

            /// <summary>
            /// The index of the layer within its layer collection (usually a SpriteComponent).
            /// </summary>
            [Access(typeof(SpriteSystem), typeof(SpriteComponent))]
            [ViewVariables] internal int Index;
            // Internal, because I might want to change this in future W/O breaking changes.

            [ViewVariables] public ProtoId<ShaderPrototype>? ShaderPrototype;
            [ViewVariables] public ShaderInstance? Shader;
            [ViewVariables] public Texture? Texture;

            internal RSI? _rsi;

            /// <summary>
            /// If true, then this layer is drawn without lighting applied.
            /// Unshaded layers are given special treatment and don't just use the unshaded-shader to avoid having to
            /// unnecessarily swap out the light texture. This helps the number of batches that need to be sent to the
            /// GPU while drawing sprites.
            /// </summary>
            [ViewVariables] internal bool UnShaded;

            [ViewVariables] public RSI? RSI
            {
                get => _rsi;
                [Obsolete("Use SpriteSystem.LayerSetRsi() instead.")]
                set
                {
                    if (_rsi == value)
                        return;

                    BoundsDirty = true;
                    Owner.Comp.BoundsDirty = true;
                    _rsi = value;
                    UpdateActualState();
                }
            }

            internal RSI.StateId StateId;
            [ViewVariables] public RSI.StateId State
            {
                get => StateId;
                [Obsolete("Use SpriteSystem.LayerSetRsiState() instead.")]
                set
                {
                    if (StateId == value)
                        return;

                    StateId = value;
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

            // TODO SPRITE ACCESS
            internal RSI.State? _actualState;
            [ViewVariables] public RSI.State? ActualState => _actualState;

            // TODO SPRITE ACCESS
            public Matrix3x2 LocalMatrix = Matrix3x2.Identity;

            [ViewVariables(VVAccess.ReadWrite)]
            public Vector2 Scale
            {
                get => _scale;
                [Obsolete("Use SpriteSystem.LayerSetScale() instead.")]
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
                    BoundsDirty = true;
                    Owner.Comp.BoundsDirty = true;
                    Owner.Comp.TreeSys.QueueTreeUpdate(Owner);
                }
            }
            internal Vector2 _scale = Vector2.One;

            [ViewVariables(VVAccess.ReadWrite)]
            public Angle Rotation
            {
                get => _rotation;
                [Obsolete("Use SpriteSystem.LayerSetRotation() instead.")]
                set
                {
                    if (_rotation.EqualsApprox(value)) return;

                    _rotation = value;
                    UpdateLocalMatrix();
                    BoundsDirty = true;
                    Owner.Comp.BoundsDirty = true;
                    Owner.Comp.TreeSys.QueueTreeUpdate(Owner);
                }
            }
            internal Angle _rotation = Angle.Zero;

            // Is the layer actually drawn / does it contribute to the sprites bounding box?
            internal bool Drawn => _visible && !Blank && CopyToShaderParameters == null;

            internal bool _visible = true;
            [ViewVariables(VVAccess.ReadWrite)]
            public bool Visible
            {
                get => _visible;
                [Obsolete("Use SpriteSystem.LayerSetVisible() instead.")]
                set
                {
                    if (_visible == value)
                        return;
                    _visible = value;

                    Owner.Comp.BoundsDirty = true;

                    // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                    if (_parent.Owner != EntityUid.Invalid)
                        Owner.Comp.Sys?.QueueUpdateIsInert(Owner);

                    if (_parent.Owner != EntityUid.Invalid)
                        Owner.Comp.TreeSys.QueueTreeUpdate(Owner);
                }
            }

            [ViewVariables]
            public bool Blank => !State.IsValid && Texture == null;

            [ViewVariables(VVAccess.ReadWrite)]
            public Color Color { get; set; } = Color.White;

            internal bool _autoAnimated = true;
            [ViewVariables(VVAccess.ReadWrite)]
            public bool AutoAnimated
            {
                get => _autoAnimated;
                [Obsolete("Use SpriteSystem.LayerSetAutoAnimated() instead.")]
                set
                {
                    if (_autoAnimated == value)
                        return;
                    _autoAnimated = value;
                    // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                    if (_parent.Owner != EntityUid.Invalid)
                        _parent.Sys?.QueueUpdateIsInert((_parent.Owner, _parent));
                }
            }

            [ViewVariables(VVAccess.ReadWrite)]
            public Vector2 Offset
            {
                get => _offset;
                [Obsolete("Use SpriteSystem.LayerSetOffset() instead.")]
                set
                {
                    if (_offset.EqualsApprox(value)) return;

                    BoundsDirty = true;
                    Owner.Comp.BoundsDirty = true;

                    _offset = value;
                    UpdateLocalMatrix();
                    // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                    if (_parent.Owner != EntityUid.Invalid)
                        Owner.Comp.TreeSys.QueueTreeUpdate(Owner);
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
            [ViewVariables] // TODO SPRITE ACCESS
            public LayerRenderingStrategy RenderingStrategy = LayerRenderingStrategy.UseSpriteStrategy;

            // TODO SPRITE ACCESS
            // If someone sets this, it stops the actual layer from being drawn, which should change the sprites bounds.
            [ViewVariables(VVAccess.ReadWrite)]
            public CopyToShaderParameters? CopyToShaderParameters;

            [Obsolete("Use SpriteSystem.AddBlankLayer")]
            public Layer(SpriteComponent parent)
            {
                Owner = (parent.Owner, parent);
            }

            internal Layer()
            {
            }

            internal Layer(Entity<SpriteComponent> owner, int index)
            {
                Owner = owner;
                Index = index;
            }

            [Obsolete] // This should be internal to SpriteSystem
            public Layer(Layer toClone, SpriteComponent parent) : this(parent)
            {
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

            // TODO SPRITE
            // Is Layer even serializable?
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

            [Obsolete("Use SpriteSystem.LayerSetAnimationTime")]
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

            [Obsolete("Use SpriteSystem.LayerSetAutoAnimated")]
            public void SetAutoAnimated(bool value)
            {
                AutoAnimated = value;

                _parent.Sys.QueueUpdateIsInert((_parent.Owner, _parent));
            }

            [Obsolete("Use SpriteSystem.LayerSetRsi")]
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

                BoundsDirty = true;
                Owner.Comp.BoundsDirty = true;

                _parent.TreeSys.QueueTreeUpdate((_parent.Owner, _parent));
                _parent.Sys.QueueUpdateIsInert((_parent.Owner, _parent));
            }

            [Obsolete("Use SpriteSystem.LayerSetRsiState")]
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
                    state = _parent.Sys.GetFallbackState();
                    Logger.ErrorS(LogCategory, "No RSI to pull new state from! Trace:\n{0}", Environment.StackTrace);
                }
                else
                {
                    if (!rsi.TryGetState(stateId, out state))
                    {
                        state = _parent.Sys.GetFallbackState();
                        Logger.ErrorS(LogCategory, "State '{0}' does not exist in RSI. Trace:\n{1}", stateId,
                            Environment.StackTrace);
                    }
                }

                AnimationFrame = 0;
                AnimationTime = 0;
                AnimationTimeLeft = state.GetDelay(0);

                _parent.Sys.QueueUpdateIsInert((_parent.Owner, _parent));
            }

            [Obsolete("Use SpriteSystem.LayerSetTexture")]
            public void SetTexture(Texture? texture)
            {
                State = default;
                Texture = texture;

                BoundsDirty = true;
                Owner.Comp.BoundsDirty = true;

                _parent.TreeSys.QueueTreeUpdate((_parent.Owner, _parent));
                _parent.Sys.QueueUpdateIsInert((_parent.Owner, _parent));
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

            /// <summary>
            /// Whether or not the layers's local bounding box is dirty and need to be rebuilt.
            /// </summary>
            internal bool BoundsDirty = true;
            internal Box2 Bounds;

            [Obsolete("Use SpriteSystem.GetLocalBounds()")]
            public Box2 CalculateBoundingBox() => Owner.Comp.Sys.GetLocalBounds(this);

            /// <summary>
            ///     Update Cached RSI state. State is cached to avoid calling this every time an entity gets drawn.
            /// </summary>
            internal void UpdateActualState()
            {
                // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
                _parent.Sys?.QueueUpdateIsInert((_parent.Owner, _parent));
                if (!State.IsValid)
                {
                    _actualState = null;
                    return;
                }

                // Pull texture from RSI state
                var rsi = RSI ?? _parent.BaseRSI;
                if (rsi == null || !rsi.TryGetState(State, out _actualState))
                {
                    _actualState = _parent.Sys?.GetFallbackState();
                }
            }

            public void GetLayerDrawMatrix(RsiDirection dir, out Matrix3x2 layerDrawMatrix)
            {
                GetLayerDrawMatrix(dir, out layerDrawMatrix, Owner.Comp.NoRotation);
            }

            /// <summary>
            /// Given the apparent rotation of an entity on screen (world + eye rotation), get layer's matrix for drawing &
            /// relevant RSI direction.
            /// </summary>
            internal void GetLayerDrawMatrix(RsiDirection dir, out Matrix3x2 layerDrawMatrix, bool noRot)
            {
                // TODO RENDERING
                // Consider changing the RSI format (or at least modify the loaded textures) to remove this
                // unnecessary matrix transformation. This transform is completely unnecessary for 4- and
                // 1-directional sprites. Its only really required for 8-directional sprites.

                if (dir == RsiDirection.South || noRot)
                    layerDrawMatrix = LocalMatrix;
                else
                    layerDrawMatrix = Matrix3x2.Multiply(_rsiDirectionMatrices[(int) dir], LocalMatrix);
            }

            private static Matrix3x2[] _rsiDirectionMatrices = new Matrix3x2[]
            {
                // array order chosen such that this array can be indexed by casing an RSI direction to an int
                Matrix3x2.Identity,
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

                if (dirType == RsiDirectionType.Dir8)
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
                    state = Sys?.GetFallbackState();
                }

                return state;
            }
        }

        [Obsolete("Use SpriteSystem.GetPrototypeTextures() instead")]
        public static IEnumerable<IDirectionalTextureProvider> GetPrototypeTextures(EntityPrototype prototype, IResourceCache resourceCache)
        {
            return GetPrototypeTextures(prototype, resourceCache, out var _);
        }

        [Obsolete("Use SpriteSystem.GetPrototypeTextures() instead")]
        public static IEnumerable<IDirectionalTextureProvider> GetPrototypeTextures(EntityPrototype prototype, IResourceCache resourceCache, out bool noRot)
        {
            var sys = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SpriteSystem>();
            return sys.GetPrototypeTextures(prototype, out noRot);
        }

        [Obsolete("Use SpriteSystem.GetPrototypeIcon() instead")]
        public static IRsiStateLike GetPrototypeIcon(EntityPrototype prototype, IResourceCache resourceCache)
        {
            var sys = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<SpriteSystem>();
            return sys.GetPrototypeIcon(prototype);
        }
    }
}
