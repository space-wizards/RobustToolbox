using Robust.Shared.GameObjects;

namespace Robust.Client.GameObjects
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedIgnorePauseComponent))]
    public sealed class IgnorePauseComponent : SharedIgnorePauseComponent
    {

    }
}
