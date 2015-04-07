using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS14.Client.Interfaces.GOC;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared;
using System;
using System.Drawing;
using System.Linq;

namespace SS14.Client.Services.UserInterface.Components
{
    internal class Hotbar : GuiComponent
    {
        private readonly IResourceManager _resourceManager;
        private readonly Sprite hotbarBG;

        private readonly GuiComponent[] slots = new GuiComponent[10];

        public Hotbar(IResourceManager resourceManager)
        {
            _resourceManager = resourceManager;
            hotbarBG = resourceManager.GetSprite("main_hotbar");
            createSlots();
            Update(0);
        }

        private void createEmpty(int slot)
        {
            slots[slot] = new HotbarSlot(_resourceManager);
            slots[slot].UserData = slot;
            ((HotbarSlot) slots[slot]).Dropped += Hotbar_Dropped;
        }

        private void assignAction(int slot, IPlayerAction act)
        {
            slots[slot] = new PlayerActionButton(act, _resourceManager);
            slots[slot].UserData = slot;
        }

        private void createSlots()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                createEmpty(i);
            }
        }

        private void Hotbar_Dropped(HotbarSlot sender)
        {
            if (IoCManager.Resolve<IUserInterfaceManager>().DragInfo.IsEntity) return;

            foreach (PlayerActionButton comp in (from a in slots where a is PlayerActionButton select a).ToArray())
            {
                if (comp.assignedAction == IoCManager.Resolve<IUserInterfaceManager>().DragInfo.DragAction)
                    createEmpty((int) comp.UserData);
            }

            assignAction((int) sender.UserData, IoCManager.Resolve<IUserInterfaceManager>().DragInfo.DragAction);
            IoCManager.Resolve<IUserInterfaceManager>().DragInfo.Reset();
        }

        public override sealed void Update(float frameTime)
        {
            hotbarBG.Position = Position;

            int y_dist = 30;
            int x_pos = 175;

            int max_x = 0;
            int max_y = 0;

            foreach (GuiComponent comp in slots)
            {
                comp.Position = new Point(Position.X + x_pos, Position.Y + y_dist);
                comp.Update(frameTime);
                if (comp.ClientArea.Right > max_x) max_x = comp.ClientArea.Right;
                if (comp.ClientArea.Bottom > max_y) max_y = comp.ClientArea.Bottom;
                x_pos += comp.ClientArea.Width + 1;
            }

            //ClientArea = new Rectangle(Position, new Size((int)max_x - Position.X + 5, (int)max_y - Position.Y + 5));
            ClientArea = Rectangle.Round(hotbarBG.AABB);
        }

        public override void Render()
        {
            //GorgonLibrary.Gorgon.CurrentRenderTarget.FilledRectangle(Position.X, Position.Y, ClientArea.Width, ClientArea.Height, Color.DimGray);
            //GorgonLibrary.Gorgon.CurrentRenderTarget.Rectangle(Position.X, Position.Y, ClientArea.Width, ClientArea.Height, Color.Black);

            hotbarBG.Draw();

            foreach (GuiComponent comp in slots)
                comp.Render();

            foreach (PlayerActionButton comp in (from a in slots where a is PlayerActionButton select a))
                comp.DrawTooltip(Point.Empty);
        }

        public override void Dispose()
        {
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            foreach (GuiComponent comp in slots)
                if (comp.MouseDown(e)) return true;
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            foreach (GuiComponent comp in slots)
                if (comp.MouseUp(e)) return true;
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            foreach (GuiComponent comp in slots)
                comp.MouseMove(e);
        }
    }
}