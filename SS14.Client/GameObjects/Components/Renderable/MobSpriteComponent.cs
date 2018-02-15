using SS14.Client.Graphics.Sprites;
using SS14.Client.Graphics;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Utility;
using System;
using SS14.Shared.Maths;
using YamlDotNet.RepresentationModel;
using SS14.Shared.Console;

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
