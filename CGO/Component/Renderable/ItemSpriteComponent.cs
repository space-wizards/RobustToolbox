using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared;
using SS3D_shared.GO;
using GorgonLibrary.Graphics;

namespace CGO
{
    public class ItemSpriteComponent : SpriteComponent
    {
        string basename = "";
        private bool IsInHand = false;
        private Hand holdingHand = Hand.None;
        public ItemSpriteComponent()
            : base()
        {
            SetDrawDepth(DrawDepth.FloorObjects);
        }

        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            base.RecieveMessage(sender, type, replies, list);

            switch (type)
            {
                case ComponentMessageType.MoveDirection:
                    if (!IsInHand)
                        break;
                    SetDrawDepth(DrawDepth.HeldItems);
                    switch ((Constants.MoveDirs)list[0])
                    {
                        case Constants.MoveDirs.north:
                            if (SpriteExists(basename + "_inhand_back"))
                                SetSpriteByKey(basename + "_inhand_back");
                            else
                                SetSpriteByKey(basename + "_inhand");
                            if (holdingHand == Hand.Left)
                                flip = false;
                            else
                                flip = true;
                            break;
                        case Constants.MoveDirs.south:
                            SetSpriteByKey(basename + "_inhand");
                            if (holdingHand == Hand.Left)
                                flip = true;
                            else
                                flip = false;
                            break;
                        case Constants.MoveDirs.east:
                            if (holdingHand == Hand.Left)
                                SetDrawDepth(DrawDepth.FloorObjects);
                            else
                                SetDrawDepth(DrawDepth.HeldItems);
                            SetSpriteByKey(basename + "_inhand_side");
                            flip = true;
                            break;
                        case Constants.MoveDirs.west:
                            if (holdingHand == Hand.Right)
                                SetDrawDepth(DrawDepth.FloorObjects);
                            else
                                SetDrawDepth(DrawDepth.HeldItems);
                            SetSpriteByKey(basename + "_inhand_side");
                            flip = false;
                            break;
                        case Constants.MoveDirs.northeast:
                            if (SpriteExists(basename + "_inhand_back"))
                                SetSpriteByKey(basename + "_inhand_back");
                            else
                                SetSpriteByKey(basename + "_inhand");
                            if (holdingHand == Hand.Left)
                                flip = false;
                            else
                                flip = true;
                            break;
                        case Constants.MoveDirs.northwest:
                            if (SpriteExists(basename + "_inhand_back"))
                                SetSpriteByKey(basename + "_inhand_back");
                            else
                                SetSpriteByKey(basename + "_inhand");
                            if (holdingHand == Hand.Left)
                                flip = false;
                            else
                                flip = true;
                            break;
                        case Constants.MoveDirs.southeast:
                            SetSpriteByKey(basename + "_inhand");
                            if (holdingHand == Hand.Right)
                                flip = false;
                            else
                                flip = true;
                            break;
                        case Constants.MoveDirs.southwest:
                            SetSpriteByKey(basename + "_inhand");
                            if (holdingHand == Hand.Right)
                                flip = false;
                            else
                                flip = true;
                            break;
                    }
                    break;
                case ComponentMessageType.Dropped:
                    SetSpriteByKey(basename);
                    IsInHand = false;
                    SetDrawDepth(DrawDepth.FloorObjects);
                    holdingHand = Hand.None;
                    break;
                case ComponentMessageType.PickedUp:
                    IsInHand = true;
                    holdingHand = (Hand)list[0];
                    break;
            }

        }

        /// <summary>
        /// Set parameters :)
        /// </summary>
        /// <param name="parameter"></param>
        public override void SetParameter(ComponentParameter parameter)
        {
            //base.SetParameter(parameter);
            switch (parameter.MemberName)
            {
                case "basename":
                    basename = (string)parameter.Parameter;
                    LoadSprites();
                    break;
            }
        }

        protected override Sprite GetBaseSprite()
        {
            return sprites[basename];
        }

        /// <summary>
        /// Load the mob sprites given the base name of the sprites.
        /// </summary>
        public void LoadSprites()
        {
            AddSprite(basename);
            AddSprite(basename + "_inhand");
            AddSprite(basename + "_inhand_side");
            if (ClientResourceManager.ResMgr.Singleton.SpriteExists(basename + "_inhand_back"))
                AddSprite(basename + "_inhand_back");

            SetSpriteByKey(basename);
        }

        protected override bool WasClicked(System.Drawing.PointF worldPos)
        {
            return base.WasClicked(worldPos) && !IsInHand;
        }
    }
}
