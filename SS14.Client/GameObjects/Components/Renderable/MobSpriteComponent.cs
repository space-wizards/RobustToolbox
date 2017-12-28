/*
using OpenTK;
using SS14.Client.Graphics.Sprites;
using SS14.Client.Graphics;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using SS14.Shared.Maths;
using YamlDotNet.RepresentationModel;
using OpenTK.Graphics;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.GameObjects
{
    public class MobSpriteComponent : SpriteComponent
    {
        public override string Name => "MobSprite";
        public override uint? NetID => NetIDs.MOB_SPRITE;
        private string _basename;
        private SpeechBubble _speechBubble;

        public MobSpriteComponent()
        {
            DrawDepth = DrawDepth.MobBase;
        }

        public override ComponentReplyMessage ReceiveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.ReceiveMessage(sender, type, list);

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

        public override void LoadParameters(YamlMappingNode mapping)
        {
            base.LoadParameters(mapping);

            YamlNode node;
            if (mapping.TryGetNode("drawdepth", out node))
            {
                SetDrawDepth(node.AsEnum<DrawDepth>());
            }

            if (mapping.TryGetNode("basename", out node))
            {
                _basename = node.AsString();
                LoadSprites();
            }

            if (mapping.TryGetNode("color", out node))
            {
                try
                {
                    Color = System.Drawing.Color.FromName(node.ToString());
                }
                catch
                {
                    Color = node.AsHexColor();
                }
            }
        }

        protected override Sprite GetBaseSprite()
        {
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
            if (!visible)
            {
                return;
            }

            var position = Owner.GetComponent<ITransformComponent>().WorldPosition;

            if (position.X < topLeft.X
                || position.X > bottomRight.X
                || position.Y < topLeft.Y
                || position.Y > bottomRight.Y)
            {
                return;
            }

            base.Render(topLeft, bottomRight);

            _speechBubble?.Draw(position * CluwneLib.Camera.PixelsPerMeter,
                                new Vector2(), currentBaseSprite);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (SpriteComponentState)state;
            base.HandleComponentState(state);

            if (newState.BaseName == null || _basename == newState.BaseName) return;
            _basename = newState.BaseName;
            LoadSprites();
        }
    }
}
*/
