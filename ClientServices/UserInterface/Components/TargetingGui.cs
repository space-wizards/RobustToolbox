using System;
using System.Drawing;
using CGO;
using ClientInterfaces;
using ClientInterfaces.Player;
using ClientInterfaces.Resource;
using ClientInterfaces.UserInterface;
using ClientInterfaces.Network;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using SS13_Shared.GO;
using SS13_Shared;
using SS13.IoC;
using GorgonLibrary.InputDevices;
using ClientServices.Helpers;

namespace ClientServices.UserInterface.Components
{
    public class TargetingGui : GuiComponent
    {
        Sprite background;

        IPlayerManager _playerManager = IoCManager.Resolve<IPlayerManager>();
        IUserInterfaceManager _userInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();
        IResourceManager _resMgr = IoCManager.Resolve<IResourceManager>();
        INetworkManager _netMgr = IoCManager.Resolve<INetworkManager>();

        TargetingDummy _targetArea;

        public TargetingGui()
            : base()
        {
            ComponentClass = GuiComponentType.TargetingUi;
            background = _resMgr.GetSprite("targetBG");
            _targetArea = new TargetingDummy(_playerManager, _netMgr, _resMgr);
        }

        public override void ComponentUpdate(params object[] args)
        {
            _targetArea.UpdateHealthIcon();
        }

        public override void Update()
        {
            background.Position = Position;
            //_targetArea.Position = new Point(Position.X + 5, Position.Y + 5);
            _targetArea.Position = new Point(Position.X + (int)(ClientArea.Width / 2f) - (int)(_targetArea.ClientArea.Width / 2f), Position.Y + 15);
            _targetArea.Update();
            ClientArea = new Rectangle(Position.X, Position.Y, (int)background.Width, (int)background.Height);
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
            GC.SuppressFinalize(this);
        }

        public override void HandleNetworkMessage(Lidgren.Network.NetIncomingMessage message)
        {
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
                return _targetArea.MouseDown(e);
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
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
