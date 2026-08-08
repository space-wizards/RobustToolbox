namespace Robust.Shared.Random;

/// <summary>
///     An interface for randomizers that ensures they're <b>not</b> the global randomizer.
///     This allows you to express the fact your API surface takes ownership of a given randomizer.
/// </summary>
public interface IDedicatedRandom : IRobustRandom;

