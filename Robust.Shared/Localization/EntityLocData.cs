using System.Collections.Immutable;

namespace Robust.Shared.Localization
{
    /// <summary>
    ///     Contains based localized entity prototype data.
    /// </summary>
    /// <param name="Name">The localized name of the entity prototype.</param>
    /// <param name="Desc">The localized description of the entity prototype.</param>
    /// <param name="Suffix">Editor-visible suffix of this entity prototype.</param>
    /// <param name="Attributes">Any extra attributes that can be used for localization, such as gender, proper, ...</param>
    public record EntityLocData(
        string Name,
        string Desc,
        string? Suffix,
        ImmutableDictionary<string, string> Attributes);
}
