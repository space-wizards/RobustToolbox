namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     An optimisation component for stuff that should be set as collidable when its awake and non-collidable when asleep.
    /// </summary>
    [RegisterComponent]
    public sealed class CollisionWakeComponent : Component
    {
        public override string Name => "CollisionWake";
    }
}
