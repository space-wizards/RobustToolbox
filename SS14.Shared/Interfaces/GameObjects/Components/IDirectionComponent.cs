using SS14.Shared.Interfaces.GameObjects;

namespace SS14.Shared.Interfaces.GameObjects.Components
{
    /// <summary>
    /// Stores the direction for an entity.
    /// </summary>
    public interface IDirectionComponent : IComponent
    {
        /// <summary>
        /// The direction that the entity is facing.
        /// </summary>
        Direction Direction { get; set; }
    }
}
