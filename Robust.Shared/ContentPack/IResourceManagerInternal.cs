namespace Robust.Shared.ContentPack;

/// <summary>
/// Internally-used interface implemented by <see cref="ResourceManager"/>.
/// Exists because the manager's initializing method shouldn't be public.
/// </summary>
internal interface IResourceManagerInternal : IResourceManager
{
    /// <summary>
    ///     Sets the manager up so that the base game can run.
    /// </summary>
    /// <param name="userData">
    /// The directory to use for user data.
    /// If null, a virtual temporary file system is used instead.
    /// </param>
    void Initialize(string? userData);
}