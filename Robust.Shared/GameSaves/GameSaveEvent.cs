using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Robust.Shared.GameSaves;

/// <summary>
/// Event that is raised before all entities from the current state are saved to file.
/// </summary>
[ByRefEvent]
public readonly record struct BeforeGameSaveEvent(ResPath SavePath);

/// <summary>
/// Event that is raised  before loading all saved entities from the file.
/// </summary>
[ByRefEvent]
public readonly record struct BeforeGameLoadEvent(ResPath SavePath);
