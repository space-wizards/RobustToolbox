using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

[ByRefEvent]
public record struct LightRenderEvent()
{
    public Texture? Mask;
    public bool MaskAutoRotate;
    public Angle Rotation;
    public float Radius;
    public float Energy;
    public Color Color;
    public bool CastShadows;
    public float Softness;
}

