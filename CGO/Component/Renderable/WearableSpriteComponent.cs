using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared;

namespace CGO
{
    public class WearableSpriteComponent : SpriteComponent
    {
        string basename = "";
        public WearableSpriteComponent()
            : base()
        {
            DrawDepth = 2; //Floor drawdepth
        }

        public override void RecieveMessage(object sender, MessageType type, params object[] list)
        {
            base.RecieveMessage(sender, type, list);

            switch (type)
            {
                case MessageType.MoveDirection:
                    switch ((Constants.MoveDirs)list[0])
                    {
                        case Constants.MoveDirs.north:
                            SetSpriteByKey(basename + "_back");
                            flip = true;
                            break;
                        case Constants.MoveDirs.south:
                            SetSpriteByKey(basename + "_front");
                            break;
                        case Constants.MoveDirs.east:
                            SetSpriteByKey(basename + "_side");
                            flip = true;
                            break;
                        case Constants.MoveDirs.west:
                            SetSpriteByKey(basename + "_side");
                            flip = false;
                            break;
                        case Constants.MoveDirs.northeast:
                            SetSpriteByKey(basename + "_back");
                            break;
                        case Constants.MoveDirs.northwest:
                            SetSpriteByKey(basename + "_back");
                            break;
                        case Constants.MoveDirs.southeast:
                            SetSpriteByKey(basename + "_front");
                            break;
                        case Constants.MoveDirs.southwest:
                            SetSpriteByKey(basename + "_front");
                            break;
                    }
                    DrawDepth = 4;
                    break;
                case MessageType.ItemDetach:
                    SetSpriteByKey(basename);
                    DrawDepth = 2;
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
