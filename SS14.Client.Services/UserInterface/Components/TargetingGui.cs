using Lidgren.Network;
using SS14.Client.Interfaces.Network;
using SS14.Client.Interfaces.Player;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared;
using SS14.Shared.IoC;
using System;
using System.Drawing;
using SS14.Client.Graphics.Sprite;
using SFML.Window;
using SFML.Graphics;
using SS14.Client.Graphics;

namespace SS14.Client.Services.UserInterface.Components
{
    public class TargetingGui : GuiComponent
    {
        private readonly INetworkManager _netMgr = IoCManager.Resolve<INetworkManager>();
        private readonly IPlayerManager _playerManager = IoCManager.Resolve<IPlayerManager>();
        private readonly IResourceManager _resMgr = IoCManager.Resolve<IResourceManager>();

        private readonly TargetingDummy _targetArea;
        private readonly Sprite background;
        private IUserInterfaceManager _userInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();

        public TargetingGui()
        {
            ComponentClass = GuiComponentType.TargetingUi;
            background = _resMgr.GetSprite("targetBG");
            _targetArea = new TargetingDummy(_playerManager, _netMgr, _resMgr);
        }

        public override void ComponentUpdate(params object[] args)
        {
            _targetArea.UpdateHealthIcon();
        }

        public override void Update(float frameTime)
        {
            background.Position = new SFML.System.Vector2f(Position.X,Position.Y);
            //_targetArea.Position = new Point(Position.X + 5, Position.Y + 5);
            _targetArea.Position =
                new Point(Position.X + (int) (ClientArea.Width/2f) - (int) (_targetArea.ClientArea.Width/2f),
                          Position.Y + 15);
            _targetArea.Update(0);

            var bounds = background.GetLocalBounds();
            ClientArea = new Rectangle(Position.X, Position.Y, (int)bounds.Width, (int)bounds.Height);
        }

        public override void Render()
        {
            background.Draw();
            _targetArea.Render();
        }

        public override void Resize()
        {
        }

        public override void Dispose()
        {
            _targetArea.Dispose();
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override void HandleNetworkMessage(NetIncomingMessage message)
        {
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.X, (int) e.Y)))
                return _targetArea.MouseDown(e);
            return false;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.X, (int) e.Y)))
            {
                return true;
            }
            return false;
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
        }

        public override bool MouseWheelMove(MouseWheelEventArgs e)
        {
            return false;
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            return false;
        }
    }
}