using Robust.Shared.GameObjects;

namespace Robust.Server.Maps;

/// <summary>
/// Added to Maps that were loaded by MapLoaderSystem. If not present then this map was created externally.
/// </summary>
[RegisterComponent]
public sealed partial class LoadedMapComponent : Component
{

}
