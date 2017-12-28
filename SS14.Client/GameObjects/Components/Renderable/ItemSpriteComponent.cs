/*
using OpenTK;
using Lidgren.Network;
using SS14.Client.Graphics.Sprites;
using SS14.Client.Interfaces.Resource;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using SS14.Shared.Maths;
using YamlDotNet.RepresentationModel;
using OpenTK.Graphics;
using SS14.Shared.Map;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.GameObjects
{
    public class ItemSpriteComponent : SpriteComponent
    {
        public override string Name => "ItemSprite";
        public override uint? NetID => NetIDs.ITEM_SPRITE;
        private bool IsInHand;
        private string basename = "";
        private InventoryLocation holdingHand = InventoryLocation.None;

        public ItemSpriteComponent()
        {
            SetDrawDepth(DrawDepth.FloorObjects);
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
                    if (!IsInHand)
                        break;
                    SetDrawDepth(DrawDepth.HeldItems);
                    switch ((Direction)list[0])
                    {
                        case Direction.North:
                            if (SpriteExists(basename + "_inhand_back"))
                                SetSpriteByKey(basename + "_inhand_back");
                            else
                                SetSpriteByKey(basename + "_inhand");
                            if (holdingHand == InventoryLocation.HandLeft)
                                HorizontalFlip = false;
                            else
                                HorizontalFlip = true;
                            break;
                        case Direction.South:
                            SetSpriteByKey(basename + "_inhand");
                            if (holdingHand == InventoryLocation.HandLeft)
                                HorizontalFlip = true;
                            else
                                HorizontalFlip = false;
                            break;
                        case Direction.East:
                            if (holdingHand == InventoryLocation.HandLeft)
                                SetDrawDepth(DrawDepth.FloorObjects);
                            else
                                SetDrawDepth(DrawDepth.HeldItems);
                            SetSpriteByKey(basename + "_inhand_side");
                            HorizontalFlip = true;
                            break;
                        case Direction.West:
                            if (holdingHand == InventoryLocation.HandRight)
                                SetDrawDepth(DrawDepth.FloorObjects);
                            else
                                SetDrawDepth(DrawDepth.HeldItems);
                            SetSpriteByKey(basename + "_inhand_side");
                            HorizontalFlip = false;
                            break;
                        case Direction.NorthEast:
                            if (SpriteExists(basename + "_inhand_back"))
                                SetSpriteByKey(basename + "_inhand_back");
                            else
                                SetSpriteByKey(basename + "_inhand");
                            if (holdingHand == InventoryLocation.HandLeft)
                                HorizontalFlip = false;
                            else
                                HorizontalFlip = true;
                            break;
                        case Direction.NorthWest:
                            if (SpriteExists(basename + "_inhand_back"))
                                SetSpriteByKey(basename + "_inhand_back");
                            else
                                SetSpriteByKey(basename + "_inhand");
                            if (holdingHand == InventoryLocation.HandLeft)
                                HorizontalFlip = false;
                            else
                                HorizontalFlip = true;
                            break;
                        case Direction.SouthEast:
                            SetSpriteByKey(basename + "_inhand");
                            if (holdingHand == InventoryLocation.HandRight)
                                HorizontalFlip = false;
                            else
                                HorizontalFlip = true;
                            break;
                        case Direction.SouthWest:
                            SetSpriteByKey(basename + "_inhand");
                            if (holdingHand == InventoryLocation.HandRight)
                                HorizontalFlip = false;
                            else
                                HorizontalFlip = true;
                            break;
                    }
                    break;
                case ComponentMessageType.Dropped:
                    SetSpriteByKey(basename);
                    IsInHand = false;
                    SetDrawDepth(DrawDepth.FloorObjects);
                    holdingHand = InventoryLocation.None;
                    break;
                case ComponentMessageType.PickedUp:
                    IsInHand = true;
                    holdingHand = (InventoryLocation)list[0];
                    break;
                case ComponentMessageType.SetBaseName:
                    basename = (string)list[0];
                    break;
            }

            return reply;
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender)
        {
            base.HandleNetworkMessage(message, sender);

            switch ((ComponentMessageType)message.MessageParameters[0])
            {
                case ComponentMessageType.SetBaseName:
                    break;
            }
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            base.LoadParameters(mapping);
            YamlNode node;
            if (mapping.TryGetNode("drawdepth", out node))
            {
                SetDrawDepth(node.AsEnum<DrawDepth>());
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

            if (mapping.TryGetNode("basename", out node))
            {
                basename = node.AsString();
                LoadSprites();
            }

            if (mapping.TryGetNode<YamlSequenceNode>("sprites", out var sequence))
            {
                foreach (YamlNode spriteNode in sequence)
                {
                    LoadSprites(spriteNode.AsString());
                }
            }
        }

        protected override Sprite GetBaseSprite()
        {
            return sprites[basename];
        }

        /// <summary>
        /// Load the mob sprites given the base name of the sprites.
        /// </summary>
        public void LoadSprites()
        {
            LoadSprites(basename);
            SetSpriteByKey(basename);
        }

        public void LoadSprites(string name)
        {
            if (!HasSprite(name))
            {
                AddSprite(name);
                AddSprite(name + "_inhand");
                AddSprite(name + "_inhand_side");
                if (IoCManager.Resolve<IResourceCache>().SpriteExists(name + "_inhand_back"))
                    AddSprite(name + "_inhand_back");
            }
        }

        public override bool WasClicked(LocalCoordinates worldPos)
        {
            return !IsInHand && base.WasClicked(worldPos);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (SpriteComponentState) state;
            base.HandleComponentState((SpriteComponentState)state);

            if (newState.BaseName == null || basename == newState.BaseName)
                return;

            basename = newState.BaseName;
            LoadSprites();
        }
    }
}
*/
