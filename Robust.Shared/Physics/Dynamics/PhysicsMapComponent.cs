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
*/

using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;
using PhysicsComponent = Robust.Shared.Physics.Components.PhysicsComponent;

namespace Robust.Shared.Physics.Dynamics;

[RegisterComponent, NetworkedComponent]
public sealed partial class PhysicsMapComponent : Component
{
    public bool AutoClearForces;

    /// <summary>
    /// When substepping the client needs to know about the first position to use for lerping.
    /// </summary>
    public readonly Dictionary<EntityUid, (EntityUid ParentUid, Vector2 LocalPosition, Angle LocalRotation)>
        LerpData = new();

    /// <summary>
    /// Keep a buffer of everything that moved in a tick. This will be used to check for physics contacts.
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<FixtureProxy, Box2> MoveBuffer = new();

    /// <summary>
    ///     All awake bodies on this map.
    /// </summary>
    [ViewVariables]
    public readonly HashSet<PhysicsComponent> AwakeBodies = new();

    /// <summary>
    ///     Store last tick's invDT
    /// </summary>
    internal float _invDt0;
}

[ByRefEvent]
public readonly struct PhysicsUpdateBeforeMapSolveEvent
{
    public readonly bool Prediction;
    public readonly PhysicsMapComponent MapComponent;
    public readonly float DeltaTime;

    public PhysicsUpdateBeforeMapSolveEvent(bool prediction, PhysicsMapComponent mapComponent, float deltaTime)
    {
        Prediction = prediction;
        MapComponent = mapComponent;
        DeltaTime = deltaTime;
    }
}

[ByRefEvent]
public readonly struct PhysicsUpdateAfterMapSolveEvent
{
    public readonly bool Prediction;
    public readonly PhysicsMapComponent MapComponent;
    public readonly float DeltaTime;

    public PhysicsUpdateAfterMapSolveEvent(bool prediction, PhysicsMapComponent mapComponent, float deltaTime)
    {
        Prediction = prediction;
        MapComponent = mapComponent;
        DeltaTime = deltaTime;
    }
}
