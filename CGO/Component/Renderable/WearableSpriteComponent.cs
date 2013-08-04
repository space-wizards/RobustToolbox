using System;
using System.Drawing;
using GorgonLibrary.Graphics;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Renderable;

namespace CGO
{
    public class WearableSpriteComponent : SpriteComponent
    {
        private string _basename = "";
        private bool worn;
        private DrawDepth wornDrawDepth = DrawDepth.MobOverClothingLayer;

        public WearableSpriteComponent()
        {
            DrawDepth = DrawDepth.FloorObjects; //Floor drawdepth
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.MoveDirection:
                    switch ((Direction) list[0])
                    {
                        case Direction.North:
                            if (worn)
                            {
                                SetSpriteByKey(_basename + "_back");
                                flip = false;
                            }
                            else
                                SetSpriteByKey(_basename);
                            break;
                        case Direction.South:
                            if (worn)
                            {
                                SetSpriteByKey(_basename + "_front");
                                flip = false;
                            }
                            else
                                SetSpriteByKey(_basename);

                            break;
                        case Direction.East:
                            if (worn)
                            {
                                SetSpriteByKey(_basename + "_side");
                                flip = true;
                            }
                            else
                                SetSpriteByKey(_basename);
                            break;
                        case Direction.West:
                            if (worn)
                            {
                                SetSpriteByKey(_basename + "_side");
                                flip = false;
                            }
                            else
                                SetSpriteByKey(_basename);
                            break;
                        case Direction.NorthEast:
                            if (worn)
                            {
                                SetSpriteByKey(_basename + "_back");
                                flip = false;
                            }
                            else
                                SetSpriteByKey(_basename);
                            break;
                        case Direction.NorthWest:
                            if (worn)
                            {
                                SetSpriteByKey(_basename + "_back");
                                flip = false;
                            }
                            else
                                SetSpriteByKey(_basename);
                            break;
                        case Direction.SouthEast:
                            if (worn)
                            {
                                SetSpriteByKey(_basename + "_front");
                                flip = false;
                            }
                            else
                                SetSpriteByKey(_basename);
                            break;
                        case Direction.SouthWest:
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
                    wornDrawDepth = (DrawDepth) list[0];
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
                case "drawdepth":
                    SetDrawDepth((DrawDepth) Enum.Parse(typeof (DrawDepth), parameter.GetValue<string>(), true));
                    break;
                case "basename":
                    _basename = parameter.GetValue<string>();
                    LoadSprites();
                    break;
            }
        }

        protected override Sprite GetBaseSprite()
        {
            return sprites[_basename];
        }

        protected override bool WasClicked(PointF worldPos)
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

        public override void HandleComponentState(dynamic state)
        {
            base.HandleComponentState((SpriteComponentState) state);

            if (state.BaseName != null && _basename != state.BaseName)
            {
                _basename = state.BaseName;
                LoadSprites();
            }
        }
    }
}