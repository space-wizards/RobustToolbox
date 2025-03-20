using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Components;

namespace Robust.Client.GameObjects;

public sealed class ShowPlayerVelocityDebugSystem : EntitySystem
{
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    internal bool Enabled
    {
        get => _label.Parent != null;
        set
        {
            if (value)
            {
                _uiManager.WindowRoot.AddChild(_label);
            }
            else
            {
                _label.Orphan();
            }
        }
    }

    private Label _label = default!;

    public override void Initialize()
    {
        base.Initialize();
        _label = new Label();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        if (!Enabled)
        {
            _label.Visible = false;
            return;
        }

        var player = _playerManager.LocalEntity;

        if (player == null || !EntityManager.TryGetComponent(player.Value, out PhysicsComponent? body))
        {
            _label.Visible = false;
            return;
        }

        var screenPos = _eyeManager.WorldToScreen(_transform.GetWorldPosition(Transform(player.Value)));
        LayoutContainer.SetPosition(_label, screenPos + new Vector2(0, 50));
        _label.Visible = true;

        _label.Text = $"Speed: {body.LinearVelocity.Length():0.00}\nLinear: {body.LinearVelocity.X:0.00}, {body.LinearVelocity.Y:0.00}\nAngular: {body.AngularVelocity}";
    }
}
