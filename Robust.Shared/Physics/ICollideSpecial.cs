namespace Robust.Shared.Physics
{
    public interface ICollideSpecial
    {
        bool PreventCollide(IPhysBody collidedwith);
    }
}
