using System;
using System.Collections.Generic;
using SS13_Shared;
using SS13_Shared.GO;
using GorgonLibrary.Graphics;
using ClientWindow;

namespace CGO
{
    public class MobSpriteComponent : SpriteComponent
    {
        string _basename;
        SpeechBubble _speechBubble;

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
                            SetSpriteByKey(_basename + "_back");
                            break;
                        case Constants.MoveDirs.south:
                            SetSpriteByKey(_basename + "_front");
                            break;
                        case Constants.MoveDirs.east:
                            SetSpriteByKey(_basename + "_side");
                            flip = true;
                            break;
                        case Constants.MoveDirs.west:
                            SetSpriteByKey(_basename + "_side");
                            flip = false;
                            break;
                        case Constants.MoveDirs.northeast:
                            SetSpriteByKey(_basename + "_back");
                            break;
                        case Constants.MoveDirs.northwest:
                            SetSpriteByKey(_basename + "_back");
                            break;
                        case Constants.MoveDirs.southeast:
                            SetSpriteByKey(_basename + "_front");
                            break;
                        case Constants.MoveDirs.southwest:
                            SetSpriteByKey(_basename + "_front");
                            break;
                    }
                    break;
                case ComponentMessageType.HealthStatus:
                    break; //TODO do stuff here, incap and dead.
                case ComponentMessageType.EntitySaidSomething:
                    ChatChannel channel;
                    if (Enum.TryParse(list[0].ToString(), true, out channel))
                    {
                        var text = list[1].ToString();
                        
                        if (channel == ChatChannel.Ingame || channel == ChatChannel.Player || channel == ChatChannel.Radio)
                        {
                            (_speechBubble ?? (_speechBubble = new SpeechBubble(Owner.Name + Owner.Uid))).SetText(text);
                        }
                    }
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
                    _basename = (string)parameter.Parameter;
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
            AddSprite(_basename + "_front");
            AddSprite(_basename + "_back");
            AddSprite(_basename + "_incap");
            AddSprite(_basename + "_side");
            AddSprite(_basename + "_dead");

            SetSpriteByKey(_basename + "_front");
        }

        public override void Render()
        {
            if (!visible) return;

            base.Render();

            if (_speechBubble != null)
                _speechBubble.Draw(Owner.Position, ClientWindowData.Singleton.ScreenOrigin, currentSprite);
        }
    }
}
