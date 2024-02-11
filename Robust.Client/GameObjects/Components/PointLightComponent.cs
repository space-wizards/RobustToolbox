using Robust.Client.Graphics;
using Robust.Shared.ComponentTrees;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects;

[RegisterComponent]
public sealed partial class PointLightComponent : SharedPointLightComponent, IComponentTreeEntry<PointLightComponent>
{
    #region Component Tree

    /// <inheritdoc />
    [ViewVariables]
    public EntityUid? TreeUid { get; set; }

    /// <inheritdoc />
    [ViewVariables]
    public DynamicTree<ComponentTreeEntry<PointLightComponent>>? Tree { get; set; }

    /// <inheritdoc />
    [ViewVariables]
    public bool AddToTree => Enabled && !ContainerOccluded;

    /// <inheritdoc />
    [ViewVariables]
    public bool TreeUpdateQueued { get; set; }

    #endregion

    /// <summary>
    ///     Set a mask texture that will be applied to the light while rendering.
    ///     The mask's red channel will be linearly multiplied.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    internal Texture? Mask;
}
