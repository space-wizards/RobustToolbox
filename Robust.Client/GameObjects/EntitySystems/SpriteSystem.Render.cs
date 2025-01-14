using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Client.GameObjects;

// This partial class contains code related to actually rendering sprites.
public sealed partial class SpriteSystem
{
    public void Render(
        Entity<SpriteComponent> sprite,
        DrawingHandleWorld drawingHandle,
        Angle eyeRotation,
        Angle worldRotation,
        Vector2 worldPosition)
    {
        Render(sprite,
            drawingHandle,
            eyeRotation,
            worldRotation,
            worldPosition,
            sprite.Comp.EnableDirectionOverride ? sprite.Comp.DirectionOverride : null);
    }

    public void Render(
        Entity<SpriteComponent> sprite,
        DrawingHandleWorld drawingHandle,
        Angle eyeRotation,
        Angle worldRotation,
        Vector2 worldPosition,
        Direction? overrideDirection)
    {
        if (!sprite.Comp.IsInert)
            _queuedFrameUpdate.Add(sprite);

        var angle = worldRotation + eyeRotation; // angle on-screen. Used to decide the direction of 4/8 directional RSIs
        angle = angle.Reduced().FlipPositive();  // Reduce the angles to fix math shenanigans

        var cardinal = Angle.Zero;

        // If we have a 1-directional sprite then snap it to try and always face it south if applicable.
        if (sprite.Comp is {NoRotation: false, SnapCardinals: true})
            cardinal = angle.GetCardinalDir().ToAngle();

        // worldRotation + eyeRotation should be the angle of the entity on-screen. If no-rot is enabled this is just set to zero.
        // However, at some point later the eye-matrix is applied separately, so we subtract -eye rotation for now:
        var entityMatrix = Matrix3Helpers.CreateTransform(worldPosition, sprite.Comp.NoRotation ? -eyeRotation : worldRotation - cardinal);
        var spriteMatrix = Matrix3x2.Multiply(sprite.Comp.LocalMatrix, entityMatrix);

        // Fast path for when all sprites use the same transform matrix
        if (!sprite.Comp.GranularLayersRendering)
        {
            foreach (var layer in sprite.Comp.Layers)
            {
                layer.Render(drawingHandle, ref spriteMatrix, angle, overrideDirection);
            }
            return;
        }

        // TODO sprite optimize angle.GetCardinalDir().ToAngle()

        //Default rendering (NoRotation = false)
        entityMatrix = Matrix3Helpers.CreateTransform(worldPosition, worldRotation);
        var transformDefault = Matrix3x2.Multiply(sprite.Comp.LocalMatrix, entityMatrix);

        //Snap to cardinals
        entityMatrix = Matrix3Helpers.CreateTransform(worldPosition, worldRotation - angle.GetCardinalDir().ToAngle());
        var transformSnap = Matrix3x2.Multiply(sprite.Comp.LocalMatrix, entityMatrix);

        //No rotation
        entityMatrix = Matrix3Helpers.CreateTransform(worldPosition, -eyeRotation);
        var transformNoRot = Matrix3x2.Multiply(sprite.Comp.LocalMatrix, entityMatrix);

        foreach (var layer in sprite.Comp.Layers)
        {
            switch (layer.RenderingStrategy)
            {
                case LayerRenderingStrategy.UseSpriteStrategy:
                    layer.Render(drawingHandle, ref spriteMatrix, angle, overrideDirection);
                    break;
                case LayerRenderingStrategy.Default:
                    layer.Render(drawingHandle, ref transformDefault, angle, overrideDirection);
                    break;
                case LayerRenderingStrategy.NoRotation:
                    layer.Render(drawingHandle, ref transformNoRot, angle, overrideDirection);
                    break;
                case LayerRenderingStrategy.SnapToCardinals:
                    layer.Render(drawingHandle, ref transformSnap, angle, overrideDirection);
                    break;
                default:
                    Log.Error($"Tried to render a layer with unknown rendering stragegy: {layer.RenderingStrategy}");
                    break;
            }
        }
    }
}
