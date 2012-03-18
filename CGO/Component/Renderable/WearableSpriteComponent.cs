using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;
using SS13_Shared.GO;
using GorgonLibrary.Graphics;

namespace CGO
{
    public class WearableSpriteComponent : SpriteComponent
    {
        string basename = "";
        private bool worn = false;
        private DrawDepth wornDrawDepth = SS13_Shared.GO.DrawDepth.MobOverClothingLayer;
        public WearableSpriteComponent()
            : base()
        {
            DrawDepth = DrawDepth.FloorObjects; //Floor drawdepth
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            var reply = base.RecieveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.MoveDirection:
                    switch ((Constants.MoveDirs)list[0])
                    {
                        case Constants.MoveDirs.north:
                            if (worn)
                            {
                                SetSpriteByKey(basename + "_back");
                                flip = true;
                            }
                            else
                                SetSpriteByKey(basename);
                            break;
                        case Constants.MoveDirs.south:
                            if (worn)
                            {
                                SetSpriteByKey(basename + "_front");
                            }
                            else
                                SetSpriteByKey(basename);
                            
                            break;
                        case Constants.MoveDirs.east:
                            if (worn)
                            {
                                SetSpriteByKey(basename + "_side");
                                flip = true;
                            }
                            else
                                SetSpriteByKey(basename);
                            break;
                        case Constants.MoveDirs.west:
                            if (worn)
                            {
                                SetSpriteByKey(basename + "_side");
                                flip = false;
                            }
                            else
                                SetSpriteByKey(basename);
                            break;
                        case Constants.MoveDirs.northeast:
                            if (worn)
                            {
                                SetSpriteByKey(basename + "_back");
                            }
                            else
                                SetSpriteByKey(basename);
                            break;
                        case Constants.MoveDirs.northwest:
                            if (worn)
                            {
                                SetSpriteByKey(basename + "_back");
                            }
                            else
                                SetSpriteByKey(basename);
                            break;
                        case Constants.MoveDirs.southeast:
                            if (worn)
                            {
                                SetSpriteByKey(basename + "_front");
                            }
                            else
                                SetSpriteByKey(basename);
                            break;
                        case Constants.MoveDirs.southwest:
                            if (worn)
                            {
                                SetSpriteByKey(basename + "_front");
                            }
                            else
                                SetSpriteByKey(basename);
                            break;
                    }
                    DrawDepth = wornDrawDepth;
                    break;
                case ComponentMessageType.ItemDetach:
                    SetSpriteByKey(basename);
                    DrawDepth = DrawDepth.FloorObjects;
                    break;
                case ComponentMessageType.ItemEquipped:
                    worn = true;
                    DrawDepth = wornDrawDepth;
                    break;
                case ComponentMessageType.ItemUnEquipped:
                    worn = false;
                    break;
                case ComponentMessageType.SetWornDrawDepth:
                    wornDrawDepth = (DrawDepth)list[0];
                    break;
            }

            return reply;
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

        protected override bool WasClicked(System.Drawing.PointF worldPos)
        {
            return base.WasClicked(worldPos) && !worn;
        }

        /// <summary>
        /// Load the mob sprites given the base name of the sprites.
        /// </summary>
        public void LoadSprites()
        {
            AddSprite(basename);
            AddSprite(basename + "_front");
            AddSprite(basename + "_back");
            AddSprite(basename + "_side");

            SetSpriteByKey(basename);
        }
    }
}
