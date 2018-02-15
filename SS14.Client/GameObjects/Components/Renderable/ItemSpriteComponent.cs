using SS14.Client.Graphics.Sprites;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;
using SS14.Shared.Map;

namespace SS14.Client.GameObjects
{
    public class ItemSpriteComponent : SpriteComponent
    {
        public override string Name => "ItemSprite";
        public override uint? NetID => NetIDs.ITEM_SPRITE;
        private bool _isInHand;
        private string _basename = "";
        private InventoryLocation _holdingHand = InventoryLocation.None;

        public ItemSpriteComponent()
        {
            SetDrawDepth(DrawDepth.FloorObjects);
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
                _basename = node.AsString();
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
            return sprites[_basename];
        }

        /// <summary>
        /// Load the mob sprites given the base name of the sprites.
        /// </summary>
        public void LoadSprites()
        {
            LoadSprites(_basename);
            SetSpriteByKey(_basename);
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
            return !_isInHand && base.WasClicked(worldPos);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (SpriteComponentState) state;
            base.HandleComponentState((SpriteComponentState)state);

            if (newState.BaseName == null || _basename == newState.BaseName)
                return;

            _basename = newState.BaseName;
            LoadSprites();
        }
    }

    public enum InventoryLocation
    {
        Any = 0,
        Inventory,
        Equipment,
        HandLeft,
        HandRight,
        None
    }
}
