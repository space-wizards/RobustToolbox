using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects;

[RegisterComponent]
public sealed partial class PointLightComponent : SharedPointLightComponent
{
    /// <summary>
    ///     Set a mask texture that will be applied to the light while rendering.
    ///     The mask's red channel will be linearly multiplied.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    internal Texture? Mask;
    // TODO POINT LIGHT
    // just make this an object? on the shared comp
    // I the server-client component split is such a headache
}
