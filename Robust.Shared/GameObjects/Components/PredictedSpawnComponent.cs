namespace Robust.Shared.GameObjects;

/// <summary>
/// Indicates the attached entity was spawn predicted and should be reconciled when the server states comes in.
/// </summary>
[RegisterComponent]
public sealed partial class PredictedSpawnComponent : Component;
