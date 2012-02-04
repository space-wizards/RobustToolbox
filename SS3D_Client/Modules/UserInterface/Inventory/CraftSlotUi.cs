using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using GorgonLibrary.GUI;
using SS3D.UserInterface;
using Lidgren.Network;
using SS3D_shared;
using SS3D_shared.GO;
using CGO;
using ClientResourceManager;
using SS3D.Modules;

namespace SS3D.UserInterface
{
    class CraftSlotUi : GuiComponent
    {
        public Entity containingEntity = null;
        private Sprite entSprite = null;
        private Sprite slotSprite = null;

        private Color currCol = Color.White;

        public delegate void CraftSlotClickHandler(CraftSlotUi sender);
        public event CraftSlotClickHandler Clicked;

        public CraftSlotUi()
            : base()
        {
            slotSprite = ResMgr.Singleton.GetSprite("slot");
        }

        public void SetEntity(Entity ent)
        {
            containingEntity = ent;
            if (containingEntity != null) entSprite = Utilities.GetSpriteComponentSprite(containingEntity);
        }

        public void ResetEntity()
        {
            containingEntity = null;
            entSprite = null;
        }

        public override void Update()
        {
            clientArea = new Rectangle(position, new Size((int)slotSprite.AABB.Width, (int)slotSprite.AABB.Height));
        }

        public override void Render()
        {
            slotSprite.Color = currCol;
            slotSprite.Draw(new Rectangle(position, new Size((int)slotSprite.AABB.Width, (int)slotSprite.AABB.Height)));
            if (entSprite != null) 
                entSprite.Draw(new Rectangle((int)(position.X + slotSprite.AABB.Width / 2f - entSprite.AABB.Width / 2f), (int)(position.Y + slotSprite.AABB.Height / 2f - entSprite.AABB.Height / 2f), (int)entSprite.Width, (int)entSprite.Height));
            slotSprite.Color = Color.White;
        }

        public override void Dispose()
        {
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (clientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
            {
                ResetEntity();
                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (clientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
            {
                if(UiManager.Singleton.dragInfo.isEntity && UiManager.Singleton.dragInfo.dragEntity != null)
                {
                    SetEntity(UiManager.Singleton.dragInfo.dragEntity);
                    UiManager.Singleton.dragInfo.Reset();
                    return true;
                }
            }
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            if (clientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
                currCol = Color.LightSteelBlue;
            else
                currCol = Color.White;
            
        }
    }
}
