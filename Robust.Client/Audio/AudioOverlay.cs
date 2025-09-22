using System.Numerics;
using System.Text;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using AudioComponent = Robust.Shared.Audio.Components.AudioComponent;

namespace Robust.Client.Audio;

/// <summary>
/// Debug overlay for audio.
/// </summary>
public sealed class AudioOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private IEntityManager _entManager;
    private IPlayerManager _playerManager;
    private AudioSystem _audio;
    private SharedTransformSystem _transform;

    private Font _font;

    public AudioOverlay(IEntityManager entManager, IPlayerManager playerManager, IResourceCache cache, AudioSystem audio, SharedTransformSystem transform)
    {
        _entManager = entManager;
        _playerManager = playerManager;
        _audio = audio;
        _transform = transform;

        _font = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);
    }

    protected internal override void Draw(in OverlayDrawArgs args)
    {
        var localPlayer = _playerManager.LocalEntity;

        if (args.ViewportControl == null || localPlayer == null)
            return;

        var screenHandle = args.ScreenHandle;
        var output = new StringBuilder();
        var listenerPos = _transform.GetMapCoordinates(_entManager.GetComponent<TransformComponent>(localPlayer.Value));

        if (listenerPos.MapId != args.MapId)
            return;

        var query = _entManager.AllEntityQueryEnumerator<AudioComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            var mapId = MapId.Nullspace;
            var audioPos = Vector2.Zero;

            if (_entManager.TryGetComponent<TransformComponent>(uid, out var xform))
            {
                mapId = xform.MapID;
                audioPos = _transform.GetWorldPosition(uid);
            }

            if (mapId != args.MapId)
                continue;

            var screenPos = args.ViewportControl.WorldToScreen(audioPos);
            var distance = audioPos - listenerPos.Position;
            var posOcclusion = _audio.GetOcclusion(listenerPos, distance, distance.Length(), uid);

            output.Clear();
            output.AppendLine("Audio Source");
            output.AppendLine("Runtime:");
            output.AppendLine($"- Distance: {_audio.GetAudioDistance(distance.Length()):0.00}");
            output.AppendLine($"- Occlusion: {posOcclusion:0.0000}");
            output.AppendLine("Params:");
            output.AppendLine($"- RolloffFactor: {comp.RolloffFactor:0.0000}");
            output.AppendLine($"- Volume: {comp.Volume:0.0000}");
            output.AppendLine($"- Reference distance: {comp.ReferenceDistance:0.00}");
            output.AppendLine($"- Max distance: {comp.MaxDistance:0.00}");
            var outputText = output.ToString().Trim();
            var dimensions = screenHandle.GetDimensions(_font, outputText, 1f);
            var buffer = new Vector2(3f, 3f);
            screenHandle.DrawRect(new UIBox2(screenPos - buffer, screenPos + dimensions + buffer), new Color(39, 39, 48));
            screenHandle.DrawString(_font, screenPos, outputText);
        }
    }
}
