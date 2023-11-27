using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.Commands;

/// <summary>
/// Shows a debug overlay for audio sources.
/// </summary>
public sealed class ShowAudioCommand : LocalizedCommands
{
    [Dependency] private readonly IResourceCache _client = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _playerMgr = default!;
    public override string Command => "showaudio";
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_overlayManager.HasOverlay<AudioOverlay>())
            _overlayManager.RemoveOverlay<AudioOverlay>();
        else
            _overlayManager.AddOverlay(new AudioOverlay(
                _entManager,
                _playerMgr,
                _client,
                _entManager.System<AudioSystem>(),
                _entManager.System<SharedTransformSystem>()));
    }
}
