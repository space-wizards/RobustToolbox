using System;
using System.Collections.Generic;
using System.Linq;
using ClientInterfaces;
using ClientInterfaces.Resource;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;
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

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            var reply = base.RecieveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return ComponentReplyMessage.Empty;

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
                case ComponentMessageType.SetBaseName:
                    basename = (string)list[0];
                    break;
            }

            return reply;

        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            base.HandleNetworkMessage(message);

            switch ((ComponentMessageType)message.MessageParameters[0])
            {
                case ComponentMessageType.SetBaseName:
                    basename = (string) message.MessageParameters[1];
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
                case "drawdepth":
                    SetDrawDepth((DrawDepth)Enum.Parse(typeof(DrawDepth), parameter.GetValue<string>(), true));
                    break;
                case "basename":
                    basename = parameter.GetValue<string>();
                    LoadSprites();
                    break;
                case "addsprite":
                    var spriteToAdd = parameter.GetValue<string>();
                    LoadSprites(spriteToAdd);
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
            LoadSprites(basename);
            SetSpriteByKey(basename);
        }

        public void LoadSprites(string name)
        {
            AddSprite(name);
            AddSprite(name + "_inhand");
            AddSprite(name + "_inhand_side");
            if(IoCManager.Resolve<IResourceManager>().SpriteExists(name + "_inhand_back"))
                AddSprite(name + "_inhand_back");
        }

        protected override bool WasClicked(System.Drawing.PointF worldPos)
        {
            return base.WasClicked(worldPos) && !IsInHand;
        }
    }
}
