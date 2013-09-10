using System;
using System.Drawing;
using ClientInterfaces.Network;
using ClientInterfaces.Player;
using ClientInterfaces.Resource;
using ClientInterfaces.UserInterface;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using Lidgren.Network;
using SS13.IoC;
using SS13_Shared;

namespace ClientServices.UserInterface.Components
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
            background.Position = Position;
            //_targetArea.Position = new Point(Position.X + 5, Position.Y + 5);
            _targetArea.Position =
                new Point(Position.X + (int) (ClientArea.Width/2f) - (int) (_targetArea.ClientArea.Width/2f),
                          Position.Y + 15);
            _targetArea.Update(0);
            ClientArea = new Rectangle(Position.X, Position.Y, (int) background.Width, (int) background.Height);
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

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
                return _targetArea.MouseDown(e);
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
            {
                return true;
            }
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
        }

        public override bool MouseWheelMove(MouseInputEventArgs e)
        {
            return false;
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            return false;
        }
    }
}