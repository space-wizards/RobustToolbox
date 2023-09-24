using System.Numerics;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

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

    public AudioOverlay(IEntityManager entManager, IPlayerManager playerManager, IClientResourceCache cache, AudioSystem audio, SharedTransformSystem transform)
    {
        _entManager = entManager;
        _playerManager = playerManager;
        _audio = audio;
        _transform = transform;

        _font = new VectorFont(cache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"), 10);
    }

    protected internal override void Draw(in OverlayDrawArgs args)
    {
        var localPlayer = _playerManager.LocalPlayer?.ControlledEntity;

        if (args.ViewportControl == null || localPlayer == null)
            return;

        var screenHandle = args.ScreenHandle;
        var output = new StringBuilder();
        var listenerPos = _entManager.GetComponent<TransformComponent>(localPlayer.Value).MapPosition;

        if (listenerPos.MapId != args.MapId)
            return;

        foreach (var stream in _audio.PlayingStreams)
        {
            MapId mapId;
            Vector2 audioPos;

            if (_entManager.TryGetComponent<TransformComponent>(stream.TrackingEntity, out var xform))
            {
                mapId = xform.MapID;
                audioPos = _transform.GetWorldPosition(stream.TrackingEntity.Value);
            }
            else if (stream.TrackingCoordinates != null)
            {
                var mapPos = stream.TrackingCoordinates.Value.ToMap(_entManager);
                mapId = mapPos.MapId;
                audioPos = mapPos.Position;
            }
            else if (stream.TrackingFallbackCoordinates != null)
            {
                var mapPos = stream.TrackingFallbackCoordinates.Value.ToMap(_entManager);
                mapId = mapPos.MapId;
                audioPos = mapPos.Position;
            }
            else
            {
                continue;
            }

            if (mapId != args.MapId)
                continue;

            var screenPos = args.ViewportControl.WorldToScreen(audioPos);
            var distance = audioPos - listenerPos.Position;
            var posOcclusion = _audio.GetOcclusion(stream, listenerPos, distance, distance.Length());
            var posVolume = _audio.GetPositionalVolume(stream, distance.Length());

            output.Clear();
            output.AppendLine("Audio Source");
            output.AppendLine("Runtime:");
            output.AppendLine($"- Occlusion: {posOcclusion:0.0000}");
            output.AppendLine($"- Volume: {posVolume:0.0000}");
            output.AppendLine("Params:");
            output.AppendLine($"- Volume: {stream.Volume:0.0000}");
            output.AppendLine($"- Reference distance: {stream.ReferenceDistance}");
            output.AppendLine($"- Max distance: {stream.MaxDistance}");
            var outputText = output.ToString().Trim();
            var dimensions = screenHandle.GetDimensions(_font, outputText, 1f);
            var buffer = new Vector2(3f, 3f);
            screenHandle.DrawRect(new UIBox2(screenPos - buffer, screenPos + dimensions + buffer), new Color(39, 39, 48));
            screenHandle.DrawString(_font, screenPos, outputText);
        }
    }
}
