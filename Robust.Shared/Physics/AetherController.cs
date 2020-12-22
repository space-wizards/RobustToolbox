namespace Robust.Shared.Physics
{
    // Sloth TODO: For now I've left FilterData out as we use layers and masks which seem cleaner than categories
    // Also Box2D uses the equivalent of system controllers rather than having a controller on each entity individually.
    public abstract class AetherController
    {

        public bool Enabled = true;
        public PhysicsMap World { get; internal set; } = default!;

        public AetherController()
        {
        }

        public abstract void Update(float dt);
    }
}
