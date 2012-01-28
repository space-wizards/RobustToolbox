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
using CGO;
using SS3D_shared.GO;
using SS3D_shared;
using ClientResourceManager;
using SS3D.Modules;

namespace SS3D.UserInterface
{
    class InventorySlotUi : GuiComponent
    {
        public delegate void InventorySlotUiDropHandler(InventorySlotUi sender, Entity dropped);
        public event InventorySlotUiDropHandler Dropped;

        public EquipmentSlot assignedSlot { get; private set; }
        public Entity currentEntity { get; private set; }

        private Color color = Color.White;

        private Sprite buttonSprite;
        private Sprite currentEntSprite;

        private TextSprite text;

        private PlayerController playerControler;

        public InventorySlotUi(EquipmentSlot slot, PlayerController controler)
            : base(controler)
        {
            assignedSlot = slot;
            playerControler = controler;
            buttonSprite = ResMgr.Singleton.GetSprite("slot");
            text = new TextSprite(slot.ToString() + "UIElementSlot", slot.ToString(), ResMgr.Singleton.GetFont("CALIBRI"));
            text.ShadowColor = Color.Black;
            text.ShadowOffset = new Vector2D(1, 1);
            text.Shadowed = true;
            text.Color = Color.White;
            Update();
        }

        public override void Update()
        {
            buttonSprite.Position = Position;
            clientArea = new Rectangle(Position, new Size((int)buttonSprite.AABB.Width, (int)buttonSprite.AABB.Height));

            text.Position = position;

            if (playerController.controlledAtom == null)
                return;

            var entity = (Entity)playerController.controlledAtom;
            EquipmentComponent equipment = (EquipmentComponent)entity.GetComponent(ComponentFamily.Equipment);

            if (equipment.equippedEntities.ContainsKey(assignedSlot))
            {
                currentEntity = equipment.equippedEntities[assignedSlot];
                currentEntSprite = Utilities.GetSpriteComponentSprite(currentEntity);
            }
            else
            {
                currentEntity = null;
                currentEntSprite = null;
            } 
        }

        public override void Render()
        {
            buttonSprite.Color = color;
            buttonSprite.Position = Position;
            buttonSprite.Draw();
            buttonSprite.Color = Color.White;

            if (currentEntSprite != null && currentEntity != null)
                currentEntSprite.Draw(new Rectangle((int)(position.X + buttonSprite.AABB.Width / 2f - currentEntSprite.AABB.Width / 2f), (int)(position.Y + buttonSprite.AABB.Height / 2f - currentEntSprite.AABB.Height / 2f), (int)currentEntSprite.Width, (int)currentEntSprite.Height));

            text.Draw();
        }

        public override void Dispose()
        {
            buttonSprite = null;
            Dropped = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (clientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
            {
                if (playerController.controlledAtom == null)
                    return false;

                var entity = (Entity)playerController.controlledAtom;
                EquipmentComponent equipment = (EquipmentComponent)entity.GetComponent(ComponentFamily.Equipment);

                if (equipment.equippedEntities.ContainsKey(assignedSlot))
                    UiManager.Singleton.dragInfo.StartDrag(equipment.equippedEntities[assignedSlot]);

                return true;
            }
            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (clientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
            {
                if (currentEntity == null && UiManager.Singleton.dragInfo.isEntity && UiManager.Singleton.dragInfo.dragEntity != null)
                {
                    if (Dropped != null) Dropped(this, UiManager.Singleton.dragInfo.dragEntity);
                    return true;
                }
            }
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            if (clientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y)))
                color = Color.LightSteelBlue;
            else
                color = Color.White;
        }

        private bool IsEmpty()
        {
            if (playerController.controlledAtom == null)
                return false;

            var entity = (Entity)playerController.controlledAtom;
            EquipmentComponent equipment = (EquipmentComponent)entity.GetComponent(ComponentFamily.Equipment);

            if (equipment.IsEmpty(assignedSlot)) return true;
            else return false;
        }
    }
}
