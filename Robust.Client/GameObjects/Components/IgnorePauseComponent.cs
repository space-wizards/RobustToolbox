using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;

namespace Robust.Client.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedIgnorePauseComponent))]
    public sealed class IgnorePauseComponent : SharedIgnorePauseComponent
    {

    }
}
