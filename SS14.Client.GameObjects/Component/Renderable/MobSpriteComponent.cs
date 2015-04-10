using SS14.Client.ClientWindow;
using SS14.Shared;
using SS14.Shared.GO;
using SS14.Shared.GO.Component.Renderable;
using System;
using SS14.Shared.Maths;
using Sprite = SS14.Client.Graphics.Sprite.CluwneSprite;
namespace SS14.Client.GameObjects
{
    public class MobSpriteComponent : SpriteComponent
    {
        private string _basename;
        private SpeechBubble _speechBubble;

        public MobSpriteComponent()
        {
            DrawDepth = DrawDepth.MobBase;
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
                            SetSpriteByKey(_basename + "_back");
                            flip = false;
                            break;
                        case Direction.South:
                            SetSpriteByKey(_basename + "_front");
                            flip = false;
                            break;
                        case Direction.East:
                            SetSpriteByKey(_basename + "_side");
                            flip = true;
                            break;
                        case Direction.West:
                            SetSpriteByKey(_basename + "_side");
                            flip = false;
                            break;
                        case Direction.NorthEast:
                            SetSpriteByKey(_basename + "_back");
                            flip = false;
                            break;
                        case Direction.NorthWest:
                            SetSpriteByKey(_basename + "_back");
                            flip = false;
                            break;
                        case Direction.SouthEast:
                            SetSpriteByKey(_basename + "_front");
                            flip = false;
                            break;
                        case Direction.SouthWest:
                            SetSpriteByKey(_basename + "_front");
                            flip = false;
                            break;
                    }
                    break;
                case ComponentMessageType.Die:
                    SetSpriteByKey(_basename + "_incap_dead");
                    flip = false;
                    break;
                case ComponentMessageType.EntitySaidSomething:
                    ChatChannel channel;
                    if (Enum.TryParse(list[0].ToString(), true, out channel))
                    {
                        string text = list[1].ToString();

                        if (channel == ChatChannel.Ingame || channel == ChatChannel.Player ||
                            channel == ChatChannel.Radio)
                        {
                            (_speechBubble ?? (_speechBubble = new SpeechBubble(Owner.Name + Owner.Uid))).SetText(text);
                        }
                    }
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
            //return sprites[basename + "_front"];
            return currentBaseSprite;
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
            AddSprite(_basename + "_incap_dead");

            SetSpriteByKey(_basename + "_front");
        }

        public override void Render(Vector2 topLeft, Vector2 bottomRight)
        {
            if (!visible) return;
            if (Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.X < topLeft.X
                || Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.X > bottomRight.X
                || Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y < topLeft.Y
                || Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y > bottomRight.Y)
                return;

            base.Render(topLeft, bottomRight);

            if (_speechBubble != null)
                _speechBubble.Draw(ClientWindowData.Singleton.WorldToScreen(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position),
                                   Vector2.Zero, currentBaseSprite);
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