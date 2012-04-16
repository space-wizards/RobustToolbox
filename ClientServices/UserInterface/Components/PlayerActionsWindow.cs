using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.Network;
using ClientInterfaces.Resource;
using GorgonLibrary;
using GorgonLibrary.InputDevices;
using Lidgren.Network;
using SS13_Shared;
using CGO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SS13.IoC;
using ClientInterfaces.UserInterface;

namespace ClientServices.UserInterface.Components
{
    class PlayerActionsWindow : Window
    {
        PlayerActionComp assignedComp;
        IUserInterfaceManager uiMgr;

        public PlayerActionsWindow(Size size, IResourceManager resourceManager, PlayerActionComp _assignedComp)
            : base("Player Abilities", size, resourceManager)
        {
            uiMgr = IoCManager.Resolve<IUserInterfaceManager>();
            assignedComp = _assignedComp;
            assignedComp.Changed += new PlayerActionComp.PlayerActionsChangedHandler(assignedComp_Changed);
            Position = new Point((int)(Gorgon.CurrentRenderTarget.Width / 2f) - (int)(ClientArea.Width / 2f), (int)(Gorgon.CurrentRenderTarget.Height / 2f) - (int)(ClientArea.Height / 2f));
            assignedComp.CheckActionList();
            PopulateList();
        }

        void assignedComp_Changed(PlayerActionComp sender)
        {
            PopulateList();
        }

        private void PopulateList()
        {
            if (assignedComp == null) return;
            components.Clear();
            int pos_y = 10;
            foreach (PlayerAction act in assignedComp.Actions)
            {
                PlayerActionButton newButt = new PlayerActionButton(act, _resourceManager);
                newButt.Position = new Point(10, pos_y);
                newButt.Update(0);
                Label newLabel = new Label(act.Name, "CALIBRI", _resourceManager);
                newLabel.Update(0);
                newLabel.Position = new Point(10 + newButt.ClientArea.Width + 5, pos_y + (int)(newButt.ClientArea.Height / 2f) - (int)(newLabel.ClientArea.Height / 2f));
                components.Add(newButt);
                components.Add(newLabel);
                pos_y += 5 + newButt.ClientArea.Height;
            }
        }

        public override void Update(float frameTime)
        {
            if (disposing || !IsVisible()) return;
            base.Update(frameTime);
        }

        public override void Render()
        {
            if (disposing || !IsVisible()) return;
            base.Render();

            foreach (PlayerActionButton actButt in (from A in components where A is PlayerActionButton select A))
                actButt.DrawTooltip(Position);
        }

        public override void Dispose()
        {
            if (disposing) return;
            base.Dispose();
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return false;
            if (base.MouseDown(e)) return true;
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return false;
            if (base.MouseUp(e)) return true;
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            if (disposing || !IsVisible()) return;
            base.MouseMove(e);
        }

        public override bool MouseWheelMove(MouseInputEventArgs e)
        {
            if (base.MouseWheelMove(e)) return true;
            return false;
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            if (base.KeyDown(e)) return true;
            return false;
        }
    }
}