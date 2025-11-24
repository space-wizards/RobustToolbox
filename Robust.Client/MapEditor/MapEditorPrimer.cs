using Robust.Client.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;

namespace Robust.Client.MapEditor;

/// <summary>
/// Responsible for automatically launching the map editor after connecting to a server.
/// </summary>
internal sealed class MapEditorPrimer : IPostInjectInit
{
    [Dependency] private readonly IPlayerManager _playerManager = null!;
    [Dependency] private readonly IEntitySystemManager _esm = null!;
    [Dependency] private readonly ITaskManager _taskManager = null!;

    private bool _isPrimed;

    public void Prime()
    {
        _isPrimed = true;
    }

    void IPostInjectInit.PostInject()
    {
        _playerManager.PlayerStatusChanged += PlayerManagerOnPlayerStatusChanged;
    }

    private void PlayerManagerOnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus != SessionStatus.InGame)
            return;

        if (!_isPrimed)
            return;

        _taskManager.RunOnMainThread(() =>
        {
            _esm.GetEntitySystem<ClientMapEditorSystem>().RequestStartEditing();
        });
    }
}
