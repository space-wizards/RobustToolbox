using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared;

namespace CGO
{
    public class MobSpriteComponent : SpriteComponent
    {
        string basename = "";
        public MobSpriteComponent()
            : base()
        {
            DrawDepth = 3;
        }

        public override ComponentReplyMessage RecieveMessage(object sender, MessageType type, params object[] list)
        {
            base.RecieveMessage(sender, type, list);

            switch (type)
            {
                case MessageType.MoveDirection:
                    switch ((Constants.MoveDirs)list[0])
                    {
                        case Constants.MoveDirs.north:
                            SetSpriteByKey(basename + "_back");
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
                    break;
                case MessageType.HealthStatus:
                    break; //TODO do stuff here, incap and dead.

            }
            return ComponentReplyMessage.Null;
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
            AddSprite(basename + "_front");
            AddSprite(basename + "_back");
            AddSprite(basename + "_incap");
            AddSprite(basename + "_side");
            AddSprite(basename + "_dead");

            SetSpriteByKey(basename + "_front");
        }
    }
}
