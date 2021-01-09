using Robust.Shared.GameObjects.Components;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Joints;
using Robust.Shared.Physics.Shapes;
using Robust.Shared.Physics.Solver;

namespace Robust.Shared.Physics
{
    /// <summary>
    /// Called for each fixture found in the query.
    /// <returns>true: Continues the query, false: Terminate the query</returns>
    /// </summary>
    public delegate bool QueryReportFixtureDelegate(Fixture fixture);

    /// <summary>
    /// Called for each fixture found in the query. You control how the ray cast
    /// proceeds by returning a float:
    /// return -1: ignore this fixture and continue
    /// return 0: terminate the ray cast
    /// return fraction: clip the ray to this point
    /// return 1: don't clip the ray and continue
    /// @param fixture the fixture hit by the ray
    /// @param point the point of initial intersection
    /// @param normal the normal vector at the point of intersection
    /// @return 0 to terminate, fraction to clip the ray for closest hit, 1 to continue
    /// </summary>
    public delegate float RayCastReportFixtureDelegate(Fixture fixture, Vector2 point, Vector2 normal, float fraction);

    /// <summary>
    /// This delegate is called when a contact is deleted
    /// </summary>
    public delegate void EndContactDelegate(Contact contact);

    /// <summary>
    /// This delegate is called when a contact is created
    /// </summary>
    public delegate bool BeginContactDelegate(Contact contact);

    public delegate void PreSolveDelegate(Contact contact, ref Manifold oldManifold);

    public delegate void PostSolveDelegate(Contact contact, ContactVelocityConstraint impulse);

    public delegate bool CollisionFilterDelegate(Fixture fixtureA, Fixture fixtureB);

    /// <summary>
    ///     The 2 proxies that are in contact as well as what grid they are in contact on.
    ///     Just because there's a broadphase overlap doesn't mean there's contact (their AABBs overlap but not their shapes)./
    /// </summary>
    /// <param name="gridId"></param>
    /// <param name="proxyA"></param>
    /// <param name="proxyB"></param>
    public delegate void BroadphaseDelegate(GridId gridId, FixtureProxy proxyA, FixtureProxy proxyB);

    public delegate bool BeforeCollisionEventHandler(Fixture sender, Fixture other);

    public delegate bool OnCollisionEventHandler(Fixture sender, Fixture other, Contact contact);

    public delegate void AfterCollisionEventHandler(Fixture sender, Fixture? other, Contact contact, ContactVelocityConstraint impulse);

    public delegate void OnSeparationEventHandler(Fixture sender, Fixture? other, Contact contact);
}
