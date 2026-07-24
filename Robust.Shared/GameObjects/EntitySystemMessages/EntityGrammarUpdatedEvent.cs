using Robust.Shared.GameObjects.Components.Localization;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Raised directed on an entity when its <see cref="GrammarComponent"> is updated.
/// </summary>
[ByRefEvent]
public readonly record struct EntityGrammarUpdatedEvent(GrammarComponent grammar);
