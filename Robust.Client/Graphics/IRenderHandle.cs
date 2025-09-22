using System;
using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics
{
    /// <remarks>
    ///     Unstable API. Likely to break hard during renderer rewrite if you rely on it.
    /// </remarks>
    public interface IRenderHandle
    {
        DrawingHandleScreen DrawingHandleScreen { get; }
        DrawingHandleWorld DrawingHandleWorld { get; }

        void RenderInRenderTarget(IRenderTarget target, Action a, Color? clearColor);

        void SetScissor(UIBox2i? scissorBox);

        /// <summary>
        /// Draws an entity.
        /// </summary>
        /// <param name="entity">The entity to draw</param>
        /// <param name="position">The local pixel position where the entity should be drawn.</param>
        /// <param name="scale">Scales the drawn entity</param>
        /// <param name="worldRot">The world rotation to use when drawing the entity.
        /// This impacts the sprites RSI direction. Null will retrieve the entity's actual rotation.
        /// </param>
        /// <param name="eyeRotation">The effective "eye" angle.
        /// This will cause the entity to be rotated, and may also affect the RSI directions.
        /// Draws the entity at some given angle.</param>
        /// <param name="overrideDirection">RSI direction override.</param>
        /// <param name="sprite">The entity's sprite component</param>
        /// <param name="xform">The entity's transform component.
        /// Only required if <see cref="overrideDirection"/> is null.</param>
        /// <param name="xformSystem">The transform system</param>
        void DrawEntity(EntityUid entity,
            Vector2 position,
            Vector2 scale,
            Angle? worldRot,
            Angle eyeRotation = default,
            Direction? overrideDirection = null,
            SpriteComponent? sprite = null,
            TransformComponent? xform = null,
            SharedTransformSystem? xformSystem = null);
    }
}
