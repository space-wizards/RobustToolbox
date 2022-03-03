using System.Collections.Generic;

namespace Robust.Client.Editor.Interface;

/// <summary>
/// Manages a set of <see cref="EditorDocker"/>s. Effectively owns them and their tabs.
/// </summary>
public sealed class DockerController
{
    private readonly List<EditorDocker> _dockers = new();

    public EditorDocker MasterDocker { get; }

    public DockerController()
    {
        MasterDocker = new EditorDocker();
        _dockers.Add(MasterDocker);
    }
}
