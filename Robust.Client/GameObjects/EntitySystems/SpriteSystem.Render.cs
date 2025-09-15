using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Clyde;
using Robust.Client.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Robust.Client.GameObjects;

// This partial class contains code related to actually rendering sprites.
public sealed partial class SpriteSystem
{
    public void RenderSprite(
        Entity<SpriteComponent> sprite,
        DrawingHandleWorld drawingHandle,
        Angle eyeRotation,
        Angle worldRotation,
        Vector2 worldPosition)
    {
        RenderSprite(sprite,
            drawingHandle,
            eyeRotation,
            worldRotation,
            worldPosition,
            sprite.Comp.EnableDirectionOverride ? sprite.Comp.DirectionOverride : null);
    }

    public void RenderSprite(
        Entity<SpriteComponent> sprite,
        DrawingHandleWorld drawingHandle,
        Angle eyeRotation,
        Angle worldRotation,
        Vector2 worldPosition,
        Direction? overrideDirection)
    {
        // TODO SPRITE RENDERING
        // Add fast path for simple sprites.
        // I.e., when a sprite is modified, check if it is "simple". If it is. cache texture information in a struct
        // and use a fast path here.
        // E.g., simple 1-directional, 1-layer sprites can basically become a direct texture draw call. (most in game items).
        // Similarly, 1-directional multi-layer sprites can become a sequence of direct draw calls (most in game walls).

        if (!sprite.Comp.IsInert)
            _queuedFrameUpdate.Add(sprite);

        var angle = worldRotation + eyeRotation; // angle on-screen. Used to decide the direction of 4/8 directional RSIs
        angle = angle.Reduced().FlipPositive();  // Reduce the angles to fix math shenanigans

        var cardinal = Angle.Zero;

        // If we have a 1-directional sprite then snap it to try and always face it south if applicable.
        if (sprite.Comp is {NoRotation: false, SnapCardinals: true})
            cardinal = angle.RoundToCardinalAngle();

        // worldRotation + eyeRotation should be the angle of the entity on-screen. If no-rot is enabled this is just set to zero.
        // However, at some point later the eye-matrix is applied separately, so we subtract -eye rotation for now:
        var entityMatrix = Matrix3Helpers.CreateTransform(worldPosition, sprite.Comp.NoRotation ? -eyeRotation : worldRotation - cardinal);
        var spriteMatrix = Matrix3x2.Multiply(sprite.Comp.LocalMatrix, entityMatrix);

        // Fast path for when all sprites use the same transform matrix
        if (!sprite.Comp.GranularLayersRendering)
        {
            foreach (var layer in sprite.Comp.Layers)
            {
                RenderLayer(layer, drawingHandle, ref spriteMatrix, angle, overrideDirection);
            }
            return;
        }

        //Default rendering (NoRotation = false)
        entityMatrix = Matrix3Helpers.CreateTransform(worldPosition, worldRotation);
        var transformDefault = Matrix3x2.Multiply(sprite.Comp.LocalMatrix, entityMatrix);

        //Snap to cardinals
        entityMatrix = Matrix3Helpers.CreateTransform(worldPosition, worldRotation - angle.RoundToCardinalAngle());
        var transformSnap = Matrix3x2.Multiply(sprite.Comp.LocalMatrix, entityMatrix);

        //No rotation
        entityMatrix = Matrix3Helpers.CreateTransform(worldPosition, -eyeRotation);
        var transformNoRot = Matrix3x2.Multiply(sprite.Comp.LocalMatrix, entityMatrix);

        foreach (var layer in sprite.Comp.Layers)
        {
            switch (layer.RenderingStrategy)
            {
                case LayerRenderingStrategy.UseSpriteStrategy:
                    RenderLayer(layer, drawingHandle, ref spriteMatrix, angle, overrideDirection);
                    break;
                case LayerRenderingStrategy.Default:
                    RenderLayer(layer, drawingHandle, ref transformDefault, angle, overrideDirection);
                    break;
                case LayerRenderingStrategy.NoRotation:
                    RenderLayer(layer, drawingHandle, ref transformNoRot, angle, overrideDirection);
                    break;
                case LayerRenderingStrategy.SnapToCardinals:
                    RenderLayer(layer, drawingHandle, ref transformSnap, angle, overrideDirection);
                    break;
                default:
                    Log.Error($"Tried to render a layer with unknown rendering stragegy: {layer.RenderingStrategy}");
                    break;
            }
        }
    }

    /// <summary>
    /// Render a layer. This assumes that the input angle is between 0 and 2pi.
    /// </summary>
    private void RenderLayer(Layer layer, DrawingHandleWorld drawingHandle, ref Matrix3x2 spriteMatrix, Angle angle, Direction? overrideDirection)
    {
        if (!layer.Visible || layer.Blank)
            return;

        var state = layer._actualState;
        var dir = state == null ? RsiDirection.South : Layer.GetDirection(state.RsiDirections, angle);

        // Set the drawing transform for this layer
        layer.GetLayerDrawMatrix(dir, out var layerMatrix, layer.Owner.Comp.NoRotation);

        // The direction used to draw the sprite can differ from the one that the angle would naively suggest,
        // due to direction overrides or offsets.
        if (overrideDirection != null && state != null)
            dir = overrideDirection.Value.Convert(state.RsiDirections);
        dir = dir.OffsetRsiDir(layer.DirOffset);

        var texture = state?.GetFrame(dir, layer.AnimationFrame) ?? layer.Texture ?? GetFallbackTexture();

        // TODO SPRITE
        // Refactor shader-param-layers to a separate layer type after layers are split into types & collections.
        // I.e., separate Layer -> RsiLayer, TextureLayer, LayerCollection, SpriteLayer, and ShaderLayer
        if (layer.CopyToShaderParameters != null)
        {
            HandleShaderLayer(layer, texture, layer.CopyToShaderParameters);
            return;
        }

        // Set the drawing transform for this layer
        var transformMatrix = Matrix3x2.Multiply(layerMatrix, spriteMatrix);
        drawingHandle.SetTransform(in transformMatrix);

        if (layer.Shader != null)
            drawingHandle.UseShader(layer.Shader);

        var layerColor = layer.Owner.Comp.color * layer.Color;
        var textureSize = texture.Size / (float) EyeManager.PixelsPerMeter;
        var quad = Box2.FromDimensions(textureSize / -2, textureSize);

        if (layer.UnShaded)
        {
            DebugTools.AssertNull(layer.Shader);
            DebugTools.Assert(layerColor is {R: >= 0, G: >= 0, B: >= 0, A: >= 0}, "Default shader should not be used with negative color modulation.");

            // Negative color modulation values are by the default shader to disable light shading.
            // Specifically we set colour = - 1 - colour
            // This is good enough to ensure that non-negative values become negative & is trivially invertible.
            layerColor = new(new Vector4(-1) - layerColor.RGBA);
        }

        drawingHandle.DrawTextureRectRegion(texture, quad, layerColor);

        if (layer.Shader != null)
            drawingHandle.UseShader(null);
    }

    /// <summary>
    /// Handle a a "fake layer" that just exists to modify the parameters of a shader being used by some other
    /// layer.
    /// </summary>
    private void HandleShaderLayer(Layer layer, Texture texture, CopyToShaderParameters @params)
    {
        // Multiple atrocities to god being committed right here.
        var otherLayerIdx = layer._parent.LayerMap[@params.LayerKey!];
        var otherLayer = layer._parent.Layers[otherLayerIdx];
        if (otherLayer.Shader is not { } shader)
            return;

        if (!shader.Mutable)
            otherLayer.Shader = shader = shader.Duplicate();

        var clydeTexture = Clyde.RenderHandle.ExtractTexture(texture, null, out var csr);

        if (@params.ParameterTexture is { } paramTexture)
            shader.SetParameter(paramTexture, clydeTexture);

        if (@params.ParameterUV is not { } paramUV)
            return;

        var sr = Clyde.RenderHandle.WorldTextureBoundsToUV(clydeTexture, csr);
        var uv = new Vector4(sr.Left, sr.Bottom, sr.Right, sr.Top);
        shader.SetParameter(paramUV, uv);
    }
}
