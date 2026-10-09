namespace Robust.Roslyn.Shared;

/// <summary>
/// Standardized categories for use in diagnostic descriptors.
/// </summary>
public static class DiagnosticCategories
{
    /// <summary>
    /// Ensuring proper structure of classes and their interactions.
    /// </summary>
    public const string Design = nameof(Design);

    /// <summary>
    /// Enforcing naming convention rules.
    /// </summary>
    public const string Naming = nameof(Naming);

    /// <summary>
    /// Preventing bad patterns that hurt performance.
    /// </summary>
    public const string Performance = nameof(Performance);

    /// <summary>
    /// Preventing vulnerabilities from bad netcode or unsafe patterns.
    /// </summary>
    public const string Security = nameof(Security);

    /// <summary>
    /// Enforcing that code is clean, readable, and compliant with conventions.
    /// </summary>
    public const string Style = nameof(Style);

    /// <summary>
    /// Ensuring proper syntax and use of code, such as argument validation or attribute application.
    /// </summary>
    public const string Usage = nameof(Usage);
}
