using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared;
using SS3D_shared.GO;
using GorgonLibrary.Graphics;

namespace CGO
{
    public class MobSpriteComponent : SpriteComponent
    {
        string basename = "";
        SpeechBubble speechBubble;
        public MobSpriteComponent()
            : base()
        {
            DrawDepth = DrawDepth.MobBase;
        }

        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            base.RecieveMessage(sender, type, replies, list);

            switch (type)
            {
                case ComponentMessageType.MoveDirection:
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
                case ComponentMessageType.HealthStatus:
                    break; //TODO do stuff here, incap and dead.
                case ComponentMessageType.EntitySaidSomething:
                    ChatChannel channel = (ChatChannel)list[0];
                    string text = (string)list[1];
                    if (speechBubble == null) speechBubble = new SpeechBubble(Owner.name + Owner.Uid.ToString());
                    if(channel == ChatChannel.Ingame || channel == ChatChannel.Player || channel == ChatChannel.Radio)
                        speechBubble.SetText(text);
                    //TODO re-enable speechbubbles
                    break;

            }
            return;
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
            //return sprites[basename + "_front"];
            return currentSprite;
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

        public override void Render()
        {
            base.Render();

            if (speechBubble != null)
                speechBubble.Draw(Owner.Position, ClientWindow.ClientWindowData.xTopLeft, ClientWindow.ClientWindowData.yTopLeft, currentSprite);
        }
    }
}
