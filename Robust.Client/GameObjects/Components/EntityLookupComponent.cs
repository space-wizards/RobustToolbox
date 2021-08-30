using Robust.Shared.GameObjects;

namespace Robust.Client.GameObjects
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedEntityLookupComponent))]
    internal sealed class EntityLookupComponent : SharedEntityLookupComponent
    {

    }
}
