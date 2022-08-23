using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedPointLightComponent))]
    public sealed class PointLightComponent : SharedPointLightComponent {}
}
