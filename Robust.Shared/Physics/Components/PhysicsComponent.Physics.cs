/*
* Farseer Physics Engine:
* Copyright (c) 2012 Ian Qvist
*
* Original source Box2D:
* Copyright (c) 2006-2011 Erin Catto http://www.box2d.org
*
* This software is provided 'as-is', without any express or implied
* warranty.  In no event will the authors be held liable for any damages
* arising from the use of this software.
* Permission is granted to anyone to use this software for any purpose,
* including commercial applications, and to alter it and redistribute it
* freely, subject to the following restrictions:
* 1. The origin of this software must not be misrepresented; you must not
* claim that you wrote the original software. If you use this software
* in a product, an acknowledgment in the product documentation would be
* appreciated but is not required.
* 2. Altered source versions must be plainly marked as such, and must not be
* misrepresented as being the original software.
* 3. This notice may not be removed or altered from any source distribution.
 *
 * PhysicsComponent is heavily modified from Box2D.
*/

using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics.Contacts;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Physics.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PhysicsComponent : Component, IComponentDelta
{
    public GameTick LastFieldUpdate { get; set; }
    public GameTick[] LastModifiedFields { get; set; }

    /// <summary>
    ///     Has this body been added to an island previously in this tick.
    /// </summary>
    [Access(typeof(SharedPhysicsSystem))]
    public bool Island;

    /// <summary>
    ///     Store the body's index within the island so we can lookup its data.
    ///     Key is Island's ID and value is our index.
    /// </summary>
    [Access(typeof(SharedPhysicsSystem))]
    public Dictionary<int, int> IslandIndex = new();

    [ViewVariables] public int ContactCount => Contacts.Count;

    /// <summary>
    ///     Linked-list of all of our contacts.
    /// </summary>
    internal readonly LinkedList<Contact> Contacts = new();

    [DataField]
    public bool IgnorePaused;

    [DataField, Access(typeof(SharedPhysicsSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
    public BodyType BodyType = BodyType.Static;

    // We'll also block Static bodies from ever being awake given they don't need to move.

    [ViewVariables(VVAccess.ReadWrite),
     Access(typeof(SharedPhysicsSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
    public bool Awake;

    /// <summary>
    /// You can disable sleeping on this body. If you disable sleeping, the
    /// body will be woken.
    /// </summary>
    /// <value><c>true</c> if sleeping is allowed; otherwise, <c>false</c>.</value>
    [DataField, Access(typeof(SharedPhysicsSystem), Friend = AccessPermissions.ReadWriteExecute,
         Other = AccessPermissions.Read)]
    public bool SleepingAllowed = true;

    [DataField, Access(typeof(SharedPhysicsSystem), Friend = AccessPermissions.ReadWriteExecute,
         Other = AccessPermissions.Read)]
    public float SleepTime = 0f;

    /// <summary>
    ///     Enables or disabled collision processing of this component.
    /// </summary>
    /// <remarks>
    ///     Also known as Enabled in Box2D
    /// </remarks>
    [DataField, Access(typeof(SharedPhysicsSystem), Friend = AccessPermissions.ReadWriteExecute,
         Other = AccessPermissions.Read)]
    public bool CanCollide = true;

    /// <summary>
    ///     Non-hard physics bodies will not cause action collision (e.g. blocking of movement)
    ///     while still raising collision events. Recommended you use the fixture hard values directly
    /// </summary>
    /// <remarks>
    ///     This is useful for triggers or such to detect collision without actually causing a blockage.
    /// </remarks>
    [ViewVariables, Access(typeof(SharedPhysicsSystem), typeof(FixtureSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
    public bool Hard { get; internal set; }

    /// <summary>
    ///     Bitmask of the collision layers this component is a part of.
    /// </summary>
    [ViewVariables, Access(typeof(SharedPhysicsSystem), typeof(FixtureSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
    public int CollisionLayer { get; internal set; }

    /// <summary>
    ///     Bitmask of the layers this component collides with.
    /// </summary>
    [ViewVariables, Access(typeof(SharedPhysicsSystem), typeof(FixtureSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
    public int CollisionMask { get; internal set; }

    /// <summary>
    ///     The current total mass of the entities fixtures in kilograms. Ignores the body type.
    /// </summary>
    [ViewVariables]
    public float FixturesMass => _mass;

    // I made Mass read-only just because overwriting it doesn't touch inertia.
    /// <summary>
    ///     Current mass of the entity in kilograms. This may be 0 depending on the body type.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float Mass => (BodyType & (BodyType.Dynamic | BodyType.KinematicController)) != 0 ? _mass : 0.0f;

    internal float _mass;

    /// <summary>
    ///     Inverse mass of the entity in kilograms (1 / Mass).
    /// </summary>
    [ViewVariables]
    public float InvMass => (BodyType & (BodyType.Dynamic | BodyType.KinematicController)) != 0 ? _invMass : 0.0f;

    internal float _invMass;

    /// <summary>
    /// Moment of inertia, or angular mass, in kg * m^2.
    /// </summary>
    /// <remarks>
    /// https://en.wikipedia.org/wiki/Moment_of_inertia
    /// </remarks>
    [ViewVariables]
    public float Inertia => _inertia + _mass * Vector2.Dot(_localCenter, _localCenter);

    [ViewVariables(VVAccess.ReadWrite), Access(typeof(SharedPhysicsSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
    // ReSharper disable once InconsistentNaming
    internal float _inertia;

    /// <summary>
    ///     Indicates whether this body ignores gravity
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public bool IgnoreGravity;

    /// <summary>
    /// Inverse moment of inertia (1 / I).
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite),
     Access(typeof(SharedPhysicsSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
    public float InvI;

    /// <summary>
    ///     Is the body allowed to have angular velocity.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField,
     Access(typeof(SharedPhysicsSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
    public bool FixedRotation = true;

    /// <summary>
    ///     Get this body's center of mass offset to world position.
    /// </summary>
    [ViewVariables]
    public Vector2 LocalCenter => _localCenter;

    [Access(typeof(SharedPhysicsSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
    // ReSharper disable once InconsistentNaming
    internal Vector2 _localCenter = Vector2.Zero;

    /// <summary>
    /// Current Force being applied to this entity in Newtons.
    /// </summary>
    /// <remarks>
    /// The force is applied to the center of mass.
    /// https://en.wikipedia.org/wiki/Force
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite), DataField,
     Access(typeof(SharedPhysicsSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
    public Vector2 Force;

    /// <summary>
    /// Current torque being applied to this entity in N*m.
    /// </summary>
    /// <remarks>
    /// The torque rotates around the Z axis on the object.
    /// https://en.wikipedia.org/wiki/Torque
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite), DataField,
     Access(typeof(SharedPhysicsSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
    public float Torque;

    /// <summary>
    ///     Contact friction between 2 bodies.
    /// </summary>
    [ViewVariables, Access(typeof(SharedPhysicsSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
    public float Friction => _friction;

    internal float _friction;

    /// <summary>
    ///     This is a set amount that the body's linear velocity is reduced by every tick.
    ///     Combined with the tile friction.
    /// </summary>
    [DataField,
     Access(typeof(SharedPhysicsSystem), Friend = AccessPermissions.ReadWriteExecute,
         Other = AccessPermissions.Read)]
    public float LinearDamping = 0.2f;

    /// <summary>
    ///     This is a set amount that the body's angular velocity is reduced every tick.
    ///     Combined with the tile friction.
    /// </summary>
    /// <returns></returns>
    [DataField,
     Access(typeof(SharedPhysicsSystem), Friend = AccessPermissions.ReadWriteExecute,
         Other = AccessPermissions.Read)]
    public float AngularDamping = 0.2f;

    // TODO: Datafield
    /// <summary>
    ///     Current linear velocity of the entity in meters per second.
    /// </summary>
    /// <remarks>
    ///     This is the velocity relative to the parent, but is defined in terms of map coordinates. I.e., if the
    ///     entity's parents are all stationary, this is the rate of change of this entity's world position (not
    ///     local position).
    /// </remarks>
    [ViewVariables(VVAccess.ReadWrite), Access(typeof(SharedPhysicsSystem), Friend = AccessPermissions.ReadWriteExecute,
        Other = AccessPermissions.ReadExecute)]
    public Vector2 LinearVelocity;

    /// <summary>
    ///     Current angular velocity of the entity in radians per sec.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), Access(typeof(SharedPhysicsSystem), Friend = AccessPermissions.ReadWriteExecute,
         Other = AccessPermissions.ReadExecute)]
    public float AngularVelocity;

    /// <summary>
    ///     Current momentum of the entity in kilogram meters per second
    /// </summary>
    [ViewVariables]
    public Vector2 Momentum => LinearVelocity * Mass;

    /// <summary>
    ///     The current status of the object
    /// </summary>
    [DataField, Access(typeof(SharedPhysicsSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
    public BodyStatus BodyStatus { get; set; }

    [ViewVariables, Access(typeof(SharedPhysicsSystem))]
    public bool Predict;
}
