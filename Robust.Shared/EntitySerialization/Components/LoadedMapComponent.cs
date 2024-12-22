using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;

namespace Robust.Shared.EntitySerialization.Components;

/// <summary>
/// Added to Maps that were loaded by <see cref="MapLoaderSystem"/>. If not present then this map was created externally.
/// </summary>
[RegisterComponent, UnsavedComponent]
public sealed partial class LoadedMapComponent : Component
{
}
