using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Reflection;
using SS3D.HelperClasses;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS3D.Modules;
using Lidgren.Network;
using CGO;
using SS3D_shared.GO;
using SS3D_shared;
using ClientResourceManager;

namespace SS3D.UserInterface
{
    public class HumanHandsGui : GuiComponent
    {
        public override Point Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
                lSprite.Position = position;
                rSprite.Position = new Point(position.X + 67, position.Y);
            }
        }

        private Sprite lSprite;
        private Entity lEntity;

        private Sprite rSprite;
        private Entity rEntity;

        private Sprite lObjectSprite;
        private Sprite rObjectSprite;
        private Sprite backgroundSprite;
        private Color inactiveColor;

        private bool lActive = true;
        private bool rActive = false;

        [Obsolete("TODO: Implement server managed active hand stuff.")]
        public HumanHandsGui(PlayerController _playerController)
            : base(_playerController)
        {
            componentClass = SS3D_shared.GuiComponentType.AppendagesComponent;
            lSprite = ResMgr.Singleton.GetSprite("l_hand");
            rSprite = ResMgr.Singleton.GetSprite("r_hand");
            backgroundSprite = ResMgr.Singleton.GetSprite("1pxwhite");
            inactiveColor = Color.FromArgb(35, 35, 35);
        }

        public override void ComponentUpdate(params object[] args)
        {
            base.ComponentUpdate(args);
            UpdateVisibleObjects();
        }

        public void UpdateVisibleObjects()
        {
            if (playerController.controlledAtom == null)
                return;

            var entity = (Entity)playerController.controlledAtom;
            HumanHandsComponent hands = (HumanHandsComponent)entity.GetComponent(ComponentFamily.Hands);

            if (hands == null) return;

            if (hands.HandSlots.Keys.Contains(Hand.Left))
            {
                Entity EntityL = hands.HandSlots[Hand.Left];
                lEntity = EntityL;
                lObjectSprite = Utilities.GetSpriteComponentSprite(EntityL);
            }
            else lObjectSprite = null;

            if (hands.HandSlots.Keys.Contains(Hand.Right))
            {
                Entity EntityR = hands.HandSlots[Hand.Right];
                rEntity = EntityR;
                rObjectSprite = Utilities.GetSpriteComponentSprite(EntityR);
            }
            else rObjectSprite = null;

            if(hands.currentHand == Hand.Left)
            {
                lActive = true;
                rActive = false;
            }
            else if (hands.currentHand == Hand.Right)
                {
                    lActive = false;
                    rActive = true;
                }
        }

        public override void Render()
        {
            backgroundSprite.Color = System.Drawing.Color.FromArgb(51, 56, 64);
            backgroundSprite.Opacity = 240;
            backgroundSprite.Draw(new Rectangle(new Point((int)lSprite.Position.X, (int)lSprite.Position.Y), new Size((int)(lSprite.Width + rSprite.Width) - 33, (int)lSprite.Height)));

            if (lActive)
            {
                lSprite.Color = Color.White;
                lSprite.Draw();
            }
            else
            {
                lSprite.Color = inactiveColor;
                lSprite.Draw();
            }

            if (rActive)
            {
                rSprite.Color = Color.White;
                rSprite.Draw();
            }
            else
            {
                rSprite.Color = inactiveColor;
                rSprite.Draw();
            }

            if (playerController.controlledAtom == null)
                return;

            var mob = (Entity)playerController.controlledAtom;

            if (lObjectSprite != null)
            {
                lObjectSprite.SetPosition(lSprite.Position.X + 10, lSprite.Position.Y + 10);
                lObjectSprite.Color = System.Drawing.Color.White;
                lObjectSprite.Rotation = 0f;
                float factor = Math.Min(50f / lObjectSprite.Width, 50f / lObjectSprite.Height);
                lObjectSprite.UniformScale = factor;
                lObjectSprite.Draw();
                lObjectSprite.UniformScale = 1f;
            }
            if (rObjectSprite != null)
            {
                rObjectSprite.SetPosition(rSprite.Position.X + 20, rSprite.Position.Y + 10);
                rObjectSprite.Color = System.Drawing.Color.White;
                rObjectSprite.Rotation = 0f;
                float factor = Math.Min(50f / rObjectSprite.Width, 50f / rObjectSprite.Height);
                rObjectSprite.UniformScale = factor;
                rObjectSprite.Draw();
                rObjectSprite.UniformScale = 1f;
            }
        }

        [Obsolete("TODO: Reimplement Drag & Drop + inventory interaction")]
        public override bool MouseDown(MouseInputEventArgs e)
        {
            System.Drawing.RectangleF mouseAABB = new System.Drawing.RectangleF(e.Position.X, e.Position.Y, 1, 1);

            Entity playerEntity = (Entity)playerController.controlledAtom;
            HumanHandsComponent equipComponent = (HumanHandsComponent)playerEntity.GetComponent(SS3D_shared.GO.ComponentFamily.Hands);

            if(mouseAABB.IntersectsWith(lSprite.AABB))
            {
                equipComponent.SendSwitchHands(Hand.Left);
                return true;
            }
            else if(mouseAABB.IntersectsWith(rSprite.AABB))
            {
                equipComponent.SendSwitchHands(Hand.Right);
                return true;
            }
            else
                return false;
        }

        public Entity GetActiveHandItem()
        {
            var entity = (Entity)playerController.controlledAtom;
            HumanHandsComponent hands = (HumanHandsComponent)entity.GetComponent(ComponentFamily.Hands);

            if(hands.HandSlots.ContainsKey(hands.currentHand) && hands.HandSlots[hands.currentHand] != null)
                return hands.HandSlots[hands.currentHand];
            return null;
        }
    }
}
