using System;
using System.Numerics;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects;

[Serializable, NetSerializable]
public sealed class PointLightComponentState : ComponentState
{
    public Color Color;

    public float Energy;

    public float Softness;

    public bool CastShadows;

    public bool Enabled;

    public float Radius;

    public Vector2 Offset;
}
