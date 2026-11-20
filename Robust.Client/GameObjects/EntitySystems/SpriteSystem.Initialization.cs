using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using static Robust.Client.GameObjects.SpriteComponent;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Robust.Client.GameObjects;

public sealed partial class SpriteSystem
{
    private void InitializeLayers(Entity<SpriteComponent> sprite)
    {
        var component = sprite.Comp;
        if (component.layerDatums.Count == 0)
            return;

        component.LayerMap.Clear();
        component.Layers.Clear();
        component.Layers.EnsureCapacity(component.layerDatums.Count);
        foreach (var datum in component.layerDatums)
        {
            var layer = new Layer(sprite, component.Layers.Count);
            component.Layers.Add(layer);
            LayerSetData(layer, datum, true);
        }

        if (sprite.Owner.Valid)
        {
            QueueUpdateIsInert(sprite);
            _tree.QueueTreeUpdate(sprite);
        }
    }

    private void LayerSetData(
        Layer layer,
        PrototypeLayerData layerDatum,
        bool initializing)
    {
        var sprite = layer.Owner;
        var component = sprite.Comp;
        DebugTools.AssertEqual(layer, layer.Owner.Comp.Layers[layer.Index]);

        if (!string.IsNullOrWhiteSpace(layerDatum.RsiPath))
        {
            var path = TextureRoot / layerDatum.RsiPath;

            if (_resourceCache.TryGetResource(path, out RSIResource? resource))
            {
                if (initializing)
                    layer._rsi = resource.RSI;
                else
                    layer.RSI = resource.RSI;
            }
            else
            {
                Logger.Error("Unable to load layer RSI '{0}'.", path);
            }
        }

        if (!string.IsNullOrWhiteSpace(layerDatum.State))
        {
            var theRsi = layer.RSI ?? component.BaseRSI;
            if (theRsi == null)
            {
                Logger.Error("Layer has no RSI to load states from. Cannot use 'state' property. ({0})",
                    layerDatum.State);
            }
            else
            {
                var stateid = new RSI.StateId(layerDatum.State);
                if (initializing)
                    layer.StateId = stateid;
                else
                    layer.State = stateid;
                if (theRsi.TryGetState(stateid, out var state))
                {
                    if (initializing)
                        layer._actualState = state;
                    // Always use south because this layer will be cached in the serializer.
                    layer.AnimationTimeLeft = state.GetDelay(0);
                    layer.AnimationTime = 0;
                    layer.AnimationFrame = 0;
                }
                else
                {
                    if (initializing)
                        layer._actualState = GetFallbackState();
                    Logger.Error($"State '{stateid}' not found in RSI: '{theRsi.Path}'.", stateid);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(layerDatum.TexturePath))
        {
            if (layer.State.IsValid)
            {
                Logger.Error("Cannot specify 'texture' on a layer if it has an RSI state specified.");
            }
            else
            {
                layer.Texture =
                    _resourceCache.GetResource<TextureResource>(TextureRoot / layerDatum.TexturePath);
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
            else if (layerDatum.Shader == UnshadedId.Id)
            {
                layer.ShaderPrototype = UnshadedId;
                layer.UnShaded = true;
                layer.Shader = null;
            }
            else if (_prototypes.TryIndex<ShaderPrototype>(layerDatum.Shader, out var prototype))
            {
                layer.ShaderPrototype = layerDatum.Shader;
                layer.Shader = prototype.Instance();
                layer.UnShaded = false;
            }
            else
            {
                Logger.Error("Shader prototype '{0}' does not exist.", layerDatum.Shader);
            }
        }

        if (layerDatum.MapKeys != null)
        {
            foreach (var keyString in layerDatum.MapKeys)
            {
                var key = ParseKey(keyString);

                if (component.LayerMap.TryGetValue(key, out var mappedIndex))
                {
                    if (mappedIndex != layer.Index)

                        Logger.Error("Duplicate layer map key definition: {0}", key);
                    continue;
                }

                component.LayerMap[key] = layer.Index;
            }
        }

        layer.RenderingStrategy = layerDatum.RenderingStrategy ?? layer.RenderingStrategy;
        layer.Cycle = layerDatum.Cycle;
        layer.Loop = layerDatum.Loop;

        layer.Color = layerDatum.Color ?? layer.Color;
        layer._rotation = layerDatum.Rotation ?? layer._rotation;
        layer._offset = layerDatum.Offset ?? layer._offset;
        layer._scale = layerDatum.Scale ?? layer._scale;
        layer.UpdateLocalMatrix();

        // If neither state: nor texture: were provided we assume that they want a blank invisible layer.
        if (initializing)
            layer._visible = layerDatum.Visible ?? layer._visible;
        else
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

        component.BoundsDirty = true;
        layer.BoundsDirty = true;

        if (!initializing && sprite.Owner != EntityUid.Invalid)
            _tree.QueueTreeUpdate(sprite);
    }

    private object ParseKey(string keyString)
    {
        if (_reflection.TryParseEnumReference(keyString, out var @enum))
            return @enum;

        return keyString;
    }

}
