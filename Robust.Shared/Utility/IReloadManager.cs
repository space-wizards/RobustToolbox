using System;

namespace Robust.Shared.Utility;

/// <summary>
/// Handles hot-reloading modified resource files such as prototypes or shaders.
/// </summary>
internal interface IReloadManager
{
    /// <summary>
    /// File that has been modified.
    /// </summary>
    public event Action<ResPath>? OnChanged;

    /// <summary>
    /// Registers the specified directory and specified file extension to subscribe to.
    /// </summary>
    internal void Register(string directory, string filter);

    /// <summary>
    /// Registers the specified directory as a <see cref="ResPath"/> and specified file extension to subscribe to.
    /// </summary>
    internal void Register(ResPath directory, string filter);

    void Initialize();
}

