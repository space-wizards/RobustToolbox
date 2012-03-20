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
        private string _basename = "";
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
                                SetSpriteByKey(_basename + "_back");
                                flip = false;
                            }
                            else
                                SetSpriteByKey(_basename);
                            break;
                        case Constants.MoveDirs.south:
                            if (worn)
                            {
                                SetSpriteByKey(_basename + "_front");
                                flip = false;
                            }
                            else
                                SetSpriteByKey(_basename);
                            
                            break;
                        case Constants.MoveDirs.east:
                            if (worn)
                            {
                                SetSpriteByKey(_basename + "_side");
                                flip = true;
                            }
                            else
                                SetSpriteByKey(_basename);
                            break;
                        case Constants.MoveDirs.west:
                            if (worn)
                            {
                                SetSpriteByKey(_basename + "_side");
                                flip = false;
                            }
                            else
                                SetSpriteByKey(_basename);
                            break;
                        case Constants.MoveDirs.northeast:
                            if (worn)
                            {
                                SetSpriteByKey(_basename + "_back");
                                flip = false;
                            }
                            else
                                SetSpriteByKey(_basename);
                            break;
                        case Constants.MoveDirs.northwest:
                            if (worn)
                            {
                                SetSpriteByKey(_basename + "_back");
                                flip = false;
                            }
                            else
                                SetSpriteByKey(_basename);
                            break;
                        case Constants.MoveDirs.southeast:
                            if (worn)
                            {
                                SetSpriteByKey(_basename + "_front");
                                flip = false;
                            }
                            else
                                SetSpriteByKey(_basename);
                            break;
                        case Constants.MoveDirs.southwest:
                            if (worn)
                            {
                                SetSpriteByKey(_basename + "_front");
                                flip = false;
                            }
                            else
                                SetSpriteByKey(_basename);
                            break;
                    }
                    DrawDepth = wornDrawDepth;
                    break;
                case ComponentMessageType.Incapacitated:
                case ComponentMessageType.WearerIsDead:
                    SetSpriteByKey(_basename + "_incap");
                    flip = false;
                    break; //TODO do stuff here, incap and dead.
                case ComponentMessageType.NotIncapacitated:
                    SetSpriteByKey(_basename + "_incap");
                    flip = false;
                    break;
                case ComponentMessageType.ItemDetach:
                    SetSpriteByKey(_basename);
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
                    _basename = (string)parameter.Parameter;
                    LoadSprites();
                    break;
            }
        }

        protected override Sprite GetBaseSprite()
        {
            return sprites[_basename];
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
            AddSprite(_basename);
            AddSprite(_basename + "_front");
            AddSprite(_basename + "_back");
            AddSprite(_basename + "_side");
            AddSprite(_basename + "_incap");

            SetSpriteByKey(_basename);
        }
    }
}
