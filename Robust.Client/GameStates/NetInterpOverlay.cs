using System;
using System.Globalization;
using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Robust.Client.GameStates;

/// <summary>
/// Debug view for the client-only render-pose cache.
/// </summary>
internal sealed partial class NetInterpOverlay : Overlay
{
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IResourceCache _resourceCache = default!;

    private readonly TransformSystem _transforms;
    private readonly Font _font;

    private EntityUid? _filter;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public NetInterpOverlay()
    {
        IoCManager.InjectDependencies(this);
        _transforms = _entityManager.System<TransformSystem>();
        _font = new VectorFont(
            _resourceCache.GetResource<FontResource>("/Fonts/NotoSans/NotoSans-Regular.ttf"),
            10);
    }

    protected internal override void Draw(in OverlayDrawArgs args)
    {
        if (args.ViewportControl == null)
            return;

        var handle = args.ScreenHandle;
        foreach (var data in _transforms.GetRenderTransformDebugData())
        {
            if ((_filter != null && data.Entity != _filter)
                || data.CoordinateSpace != args.MapUid)
                continue;

            var simulation = args.ViewportControl.WorldToScreen(data.Simulation.Position);
            var rendered = args.ViewportControl.WorldToScreen(data.Rendered.Position);
            var source = args.ViewportControl.WorldToScreen(data.Source.Position);
            var target = args.ViewportControl.WorldToScreen(data.Target.Position);

            handle.DrawLine(source, target, Color.Yellow);
            handle.DrawLine(simulation, rendered, Color.Cyan);
            DrawMarker(handle, source, Color.Yellow);
            DrawMarker(handle, target, Color.Green);
            DrawMarker(handle, simulation, Color.Red);
            DrawMarker(handle, rendered, Color.Cyan);

            var correctionActive = data.CorrectionTranslation != Vector2.Zero || data.CorrectionRotation != Angle.Zero;
            var text = Loc.GetString("cmd-renderlerp-overlay",
                ("entity", data.Entity.ToString()),
                ("type", Loc.GetString(data.Type switch
                {
                    RenderInterpolationType.NetworkInterpolation => "cmd-renderlerp-type-network",
                    RenderInterpolationType.PredictionInterpolation => "cmd-renderlerp-type-prediction",
                    RenderInterpolationType.PredictionCorrection => "cmd-renderlerp-type-correction",
                    _ => throw new ArgumentOutOfRangeException()
                })),
                ("alpha", data.Alpha.ToString("0.000")),
                ("correction", Loc.GetString(correctionActive
                    ? "cmd-renderlerp-correction-active"
                    : "cmd-renderlerp-correction-inactive")),
                ("errorX", data.CorrectionTranslation.X.ToString("0.000")),
                ("errorY", data.CorrectionTranslation.Y.ToString("0.000")),
                ("errorRotation", data.CorrectionRotation.Degrees.ToString("0.00")),
                ("simulation", Format(data.Simulation)),
                ("rendered", Format(data.Rendered)),
                ("source", Format(data.Source)),
                ("target", Format(data.Target)),
                ("parent", data.Parent.ToString()),
                ("coordinates", data.CoordinateSpace.ToString()),
                ("sourceSpace", data.SourceRenderSpace.ToString()),
                ("targetSpace", data.TargetRenderSpace.ToString()),
                ("spaceAlpha", data.Rendered.RenderSpaceAlpha.ToString("0.000")));
            var dimensions = handle.GetDimensions(_font, text, 1f);
            var labelPos = rendered + new Vector2(8f, 8f);
            handle.DrawRect(UIBox2.FromDimensions(labelPos - new Vector2(2f), dimensions + new Vector2(4f)),
                new Color(20, 20, 24, 220));
            handle.DrawString(_font, labelPos, text);
        }
    }

    private static string Format(in RenderTransform pose)
        => Loc.GetString("cmd-renderlerp-overlay-pose",
            ("x", pose.Position.X.ToString("0.00")),
            ("y", pose.Position.Y.ToString("0.00")),
            ("rotation", pose.Rotation.Degrees.ToString("0.0")));

    private static void DrawMarker(DrawingHandleScreen handle, Vector2 position, Color color)
    {
        handle.DrawRect(UIBox2.FromDimensions(position - new Vector2(2f), new Vector2(4f)), color);
    }

    private sealed partial class NetShowInterpCommand : LocalizedCommands
    {
        [Dependency] private IOverlayManager _overlay = default!;
        [Dependency] private IPlayerManager _players = default!;

        public override string Command => "net_draw_interp";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
            => SetOverlay(shell, args, _overlay, _players, Help);
    }

    private sealed partial class RenderLerpCommand : LocalizedCommands
    {
        [Dependency] private IOverlayManager _overlay = default!;
        [Dependency] private IPlayerManager _players = default!;

        public override string Command => "renderlerp";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
            => SetOverlay(shell, args, _overlay, _players, Help);
    }

    private static void SetOverlay(
        IConsoleShell shell,
        string[] args,
        IOverlayManager overlayManager,
        IPlayerManager players,
        string help)
    {
        if (args.Length > 1)
        {
            shell.WriteError(help);
            return;
        }

        if (args.Length == 0)
        {
            if (overlayManager.HasOverlay<NetInterpOverlay>())
            {
                overlayManager.RemoveOverlay<NetInterpOverlay>();
                shell.WriteLine(Loc.GetString("cmd-renderlerp-disabled"));
            }
            else
            {
                overlayManager.AddOverlay(new NetInterpOverlay());
                shell.WriteLine(Loc.GetString("cmd-renderlerp-enabled"));
            }

            return;
        }

        if (args[0] == "0")
        {
            overlayManager.RemoveOverlay<NetInterpOverlay>();
            shell.WriteLine(Loc.GetString("cmd-renderlerp-disabled"));
            return;
        }

        EntityUid? filter;
        if (args[0].Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            filter = null;
        }
        else if (args[0].Equals("self", StringComparison.OrdinalIgnoreCase))
        {
            if (players.LocalEntity is not { } player)
            {
                shell.WriteError(Loc.GetString("cmd-renderlerp-error-no-entity"));
                return;
            }

            filter = player;
        }
        else if (EntityUid.TryParse(args[0], out var uid))
        {
            filter = uid;
        }
        else
        {
            shell.WriteError(help);
            return;
        }

        if (!overlayManager.TryGetOverlay<NetInterpOverlay>(out var overlay))
        {
            overlay = new NetInterpOverlay();
            overlayManager.AddOverlay(overlay);
        }

        overlay._filter = filter;
        shell.WriteLine(filter == null
            ? Loc.GetString("cmd-renderlerp-enabled-all")
            : Loc.GetString("cmd-renderlerp-enabled-entity", ("entity", filter.Value.ToString())));
    }

    private sealed partial class NetMispredictCommand : LocalizedCommands
    {
        [Dependency] private IEntityManager _entities = default!;
        [Dependency] private IPlayerManager _players = default!;
        [Dependency] private IRobustRandom _random = default!;

        public override string Command => "net_mispredict";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var active = false;
            var offset = 0;
            if (args.Length > 0 && args[0].Equals("active", StringComparison.OrdinalIgnoreCase))
            {
                active = true;
                offset = 1;
            }

            float x;
            float y;
            var rotationDegrees = 0f;
            var remaining = args.Length - offset;
            if (remaining == 0)
            {
                var randomOffset = _random.NextVector2(0.5f, 1f);
                x = randomOffset.X;
                y = randomOffset.Y;
            }
            else if (remaining is < 2 or > 3
                     || !float.TryParse(args[offset], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                     || !float.TryParse(args[offset + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out y)
                     || remaining == 3
                     && !float.TryParse(args[offset + 2], NumberStyles.Float, CultureInfo.InvariantCulture, out rotationDegrees))
            {
                shell.WriteError(Help);
                return;
            }

            if (_players.LocalEntity is not { } player
                || !_entities.TryGetComponent(player, out TransformComponent? xform))
            {
                shell.WriteError(Loc.GetString("cmd-net_mispredict-error-no-transform"));
                return;
            }

            var transforms = _entities.System<TransformSystem>();
            var (position, rotation) = transforms.GetWorldPositionRotation(xform);
            var rotationOffset = Angle.FromDegrees(rotationDegrees);

            if (active)
            {
                transforms.SetWorldPositionRotationPreservingRenderTransform(
                    player,
                    position + new Vector2(x, y),
                    rotation + rotationOffset,
                    xform);
            }
            else
            {
                transforms.SetWorldPositionRotation(player, position + new Vector2(x, y), rotation + rotationOffset, xform);
                transforms.SnapRenderTransform(player, true);
            }

            shell.WriteLine(Loc.GetString("cmd-net_mispredict-moved",
                ("entity", player.ToString()),
                ("x", x.ToString("0.###")),
                ("y", y.ToString("0.###")),
                ("rotation", rotationOffset.Degrees.ToString("0.###"))));
        }
    }

}
