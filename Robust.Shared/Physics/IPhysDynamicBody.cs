using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    public interface IPhysDynamicBody
    {
        ICollidableComponent Collidable { get; }
        ITransformComponent Transform { get; }

        /// <summary>
        ///     Current mass of the entity in kilograms.
        /// </summary>
        float Mass { get; set; }

        /// <summary>
        ///     Current linear velocity of the entity in meters per second.
        /// </summary>
        Vector2 LinearVelocity { get; set; }

        /// <summary>
        ///     Current angular velocity of the entity in radians per sec.
        /// </summary>
        float AngularVelocity { get; set; }

        bool EdgeSlide { get; set; }
        bool Anchored { get; set; }

        bool DidMovementCalculations { get; set; }
        List<IPhysDynamicBody> GetVelocityConsumers();
        void AddVelocityConsumer(IPhysDynamicBody physicsComponent);
        void ClearVelocityConsumers();
        List<IPhysDynamicBody> VelocityConsumers { get; }
    }
}
