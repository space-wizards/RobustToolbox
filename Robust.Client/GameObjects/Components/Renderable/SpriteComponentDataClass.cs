using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Shaders;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Renderable;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Robust.Client.GameObjects.Components.Renderable
{
    public class SpriteComponentDataClass
    {
        [CustomYamlField("layers")]
        private List<SpriteComponent.Layer>? layers;
        [CustomYamlField("layermap")]
        private Dictionary<object, int>? layermap;
        [CustomYamlField("rsi")]
        public RSI? rsi;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            var resourceCache = IoCManager.Resolve<IResourceCache>();

            // TODO: Writing?
            if (!serializer.Reading)
            {
                return;
            }

            {
                var rsi = serializer.ReadDataField<string?>("sprite", null);
                if (!string.IsNullOrWhiteSpace(rsi))
                {
                    var rsiPath = SpriteComponent.TextureRoot / rsi;
                    try
                    {
                        this.rsi = resourceCache.GetResource<RSIResource>(rsiPath).RSI;
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorS(SpriteComponent.LogCategory, "Unable to load RSI '{0}'. Trace:\n{1}", rsiPath, e);
                    }
                }
            }

            layers = new List<SpriteComponent.Layer>();

            layermap = new Dictionary<object, int>();

            var layerData =
                serializer.ReadDataField("layers", new List<SharedSpriteComponent.PrototypeLayerData>());

            if(layerData.Count == 0){
                var baseState = serializer.ReadDataField<string?>("state", null);
                var texturePath = serializer.ReadDataField<string?>("texture", null);

                if (baseState != null || texturePath != null)
                {
                    layerData.Insert(0, new SharedSpriteComponent.PrototypeLayerData
                    {
                        TexturePath = string.IsNullOrWhiteSpace(texturePath) ? null : texturePath,
                        State = string.IsNullOrWhiteSpace(baseState) ? null : baseState,
                        Color = Color.White,
                        Scale = Vector2.One,
                        Visible = true,
                    });
                }
            }

            foreach (var layerDatum in layerData)
            {
                var anyTextureAttempted = false;
                var layer = new SpriteComponent.Layer(null!);
                if (!string.IsNullOrWhiteSpace(layerDatum.RsiPath))
                {
                    var path = SpriteComponent.TextureRoot / layerDatum.RsiPath;
                    try
                    {
                        layer.RSI = resourceCache.GetResource<RSIResource>(path).RSI;
                    }
                    catch
                    {
                        Logger.ErrorS(SpriteComponent.LogCategory, "Unable to load layer RSI '{0}'.", path);
                    }
                }

                if (!string.IsNullOrWhiteSpace(layerDatum.State))
                {
                    anyTextureAttempted = true;
                    var theRsi = layer.RSI ?? rsi;
                    if (theRsi == null)
                    {
                        Logger.ErrorS(SpriteComponent.LogCategory,
                            "Layer has no RSI to load states from."
                            + "cannot use 'state' property. ({0})", layerDatum.State);
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
                            Logger.ErrorS(SpriteComponent.LogCategory,
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
                        Logger.ErrorS(SpriteComponent.LogCategory,
                            "Cannot specify 'texture' on a layer if it has an RSI state specified."
                        );
                    }
                    else
                    {
                        layer.Texture =
                            resourceCache.GetResource<TextureResource>(SpriteComponent.TextureRoot / layerDatum.TexturePath);
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
                        Logger.ErrorS(SpriteComponent.LogCategory,
                            "Shader prototype '{0}' does not exist.",
                            layerDatum.Shader);
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
                        if (IoCManager.Resolve<IReflectionManager>().TryParseEnumReference(keyString, out var @enum))
                        {
                            key = @enum;
                        }
                        else
                        {
                            key = keyString;
                        }

                        if (layermap.ContainsKey(key))
                        {
                            Logger.ErrorS(SpriteComponent.LogCategory, "Duplicate layer map key definition: {0}", key);
                            continue;
                        }

                        layermap.Add(key, index);
                    }
                }
            }
        }
    }
}
