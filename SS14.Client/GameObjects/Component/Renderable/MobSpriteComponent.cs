using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Renderable;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace SS14.Client.GameObjects
{
    [IoCTarget]
    public class MobSpriteComponent : SpriteComponent
    {
        public override string Name => "MobSprite";
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
                    switch ((Direction)list[0])
                    {
                        case Direction.North:
                            SetSpriteByKey(_basename + "_back");
                            HorizontalFlip = false;
                            break;
                        case Direction.South:
                            SetSpriteByKey(_basename + "_front");
                            HorizontalFlip = false;
                            break;
                        case Direction.East:
                            SetSpriteByKey(_basename + "_side");
                            HorizontalFlip = true;
                            break;
                        case Direction.West:
                            SetSpriteByKey(_basename + "_side");
                            HorizontalFlip = false;
                            break;
                        case Direction.NorthEast:
                            SetSpriteByKey(_basename + "_back");
                            HorizontalFlip = false;
                            break;
                        case Direction.NorthWest:
                            SetSpriteByKey(_basename + "_back");
                            HorizontalFlip = false;
                            break;
                        case Direction.SouthEast:
                            SetSpriteByKey(_basename + "_front");
                            HorizontalFlip = false;
                            break;
                        case Direction.SouthWest:
                            SetSpriteByKey(_basename + "_front");
                            HorizontalFlip = false;
                            break;
                    }
                    break;
                case ComponentMessageType.Die:
                    SetSpriteByKey(_basename + "_incap_dead");
                    HorizontalFlip = false;
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

        public override void LoadParameters(Dictionary<string, YamlNode> mapping)
        {
            base.LoadParameters(mapping);

            YamlNode node;
            if (mapping.TryGetValue("drawdepth", out node))
            {
                SetDrawDepth(node.AsEnum<DrawDepth>());
            }

            if (mapping.TryGetValue("drawdepth", out node))
            {
                _basename = node.AsString();
                LoadSprites();
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

        public override void Render(Vector2f topLeft, Vector2f bottomRight)
        {
            if (!visible) return;
            if (Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.X < topLeft.X
                || Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.X > bottomRight.X
                || Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y < topLeft.Y
                || Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y > bottomRight.Y)
                return;

            base.Render(topLeft, bottomRight);

            if (_speechBubble != null)
                _speechBubble.Draw(CluwneLib.WorldToScreen(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position),
                                   new Vector2f(), currentBaseSprite);
        }

        public override void HandleComponentState(dynamic state)
        {
            base.HandleComponentState((SpriteComponentState)state);

            if (state.BaseName != null && _basename != state.BaseName)
            {
                _basename = state.BaseName;
                LoadSprites();
            }
        }
    }
}
