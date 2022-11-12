using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;

namespace Robust.Shared.Physics.Systems;

public abstract partial class SharedPhysicsSystem
{
    /// <summary>
    ///     Used to prevent bodies from colliding; may lie depending on joints.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    internal bool ShouldCollide(PhysicsComponent body, PhysicsComponent other)
    {
        if (((body.BodyType & (BodyType.Kinematic | BodyType.Static)) != 0 &&
            (other.BodyType & (BodyType.Kinematic | BodyType.Static)) != 0) ||
            // Kinematic controllers can't collide.
            (body.BodyType == BodyType.KinematicController &&
             other.BodyType == BodyType.KinematicController))
        {
            return false;
        }

        // Does a joint prevent collision?
        // if one of them doesn't have jointcomp then they can't share a common joint.
        // otherwise, only need to iterate over the joints of one component as they both store the same joint.
        if (TryComp(body.Owner, out JointComponent? jointComponentA) &&
            TryComp(other.Owner, out JointComponent? jointComponentB))
        {
            var aUid = jointComponentA.Owner;
            var bUid = jointComponentB.Owner;

            foreach (var (_, joint) in jointComponentA.Joints)
            {
                // Check if either: the joint even allows collisions OR the other body on the joint is actually the other body we're checking.
                if (!joint.CollideConnected &&
                    ((aUid == joint.BodyAUid &&
                     bUid == joint.BodyBUid) ||
                    (bUid == joint.BodyAUid &&
                     aUid == joint.BodyBUid))) return false;
            }
        }

        var preventCollideMessage = new PreventCollideEvent(body, other);
        RaiseLocalEvent(body.Owner, ref preventCollideMessage);

        if (preventCollideMessage.Cancelled) return false;

        preventCollideMessage = new PreventCollideEvent(other, body);
        RaiseLocalEvent(other.Owner, ref preventCollideMessage);

        if (preventCollideMessage.Cancelled) return false;

        return true;
    }
}
