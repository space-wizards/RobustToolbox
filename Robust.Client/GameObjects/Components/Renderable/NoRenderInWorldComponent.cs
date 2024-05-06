using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Client.GameObjects;

[RegisterComponent]
public sealed partial class NoRenderInWorldComponent : Component
{
    [DataField] public bool Enabled = true;
}
