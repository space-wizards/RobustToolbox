using System.Collections.Generic;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Shared.Physics
{
    /// <summary>
    ///
    /// </summary>
    public interface IPhysBody
    {
        /// <summary>
        ///     Should the client update this body in FrameUpdates?
        /// </summary>
        bool Predict { get; }

        List<Fixture> FixtureList { get; }

        IEntity Owner { get; }

        Box2 WorldAABB { get; }

        int CollisionLayer { get; }

        int CollisionMask { get; }

        /// <summary>
        ///     Relative to our grid
        /// </summary>
        Vector2 LinearVelocity { get; }

        /// <summary>
        ///     Relative to our grid
        /// </summary>
        float AngularVelocity { get; }

        GameTick LastModifiedTick { get; }
    }
}
