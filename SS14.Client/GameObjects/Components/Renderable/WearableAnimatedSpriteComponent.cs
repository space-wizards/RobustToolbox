/*
using OpenTK;
using OpenTK.Graphics;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Sprites;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.GameObjects
{
    public class WearableAnimatedSpriteComponent : AnimatedSpriteComponent
    {
        public override string Name => "WearableAnimatedSprite";
        public override uint? NetID => NetIDs.WEARABLE_ANIMATED_SPRITE;
        public bool IsCurrentlyWorn { get; set; }
        public Sprite NotWornSprite { get; set; }
        public bool IsCurrentlyCarried { get; set; }
        public string CarriedSprite { get; set; }

        public override Type StateType => typeof(WearableAnimatedSpriteComponentState);

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState state)
        {
            base.HandleComponentState(state);
            IsCurrentlyWorn = ((WearableAnimatedSpriteComponentState)state).IsCurrentlyWorn;
        }

        public void SetNotWornSprite(string spritename)
        {
            NotWornSprite = IoCManager.Resolve<IResourceCache>().GetSprite(spritename);
        }

        public void SetCarriedSprite(string spritename)
        {
            CarriedSprite = spritename;
        }

        public override Sprite GetCurrentSprite()
        {
            if (!IsCurrentlyWorn)
                return NotWornSprite;
            return base.GetCurrentSprite();
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            base.LoadParameters(mapping);

            YamlNode node;
            if (mapping.TryGetNode("notWornSprite", out node))
            {
                SetNotWornSprite(node.AsString());
            }

            if (mapping.TryGetNode("carriedSprite", out node))
            {
                SetCarriedSprite(node.AsString());
            }
        }

        public override Box2 LocalAABB
        {
            get
            {
                if (!IsCurrentlyWorn)
                {
                    var bounds = GetCurrentSprite().LocalBounds;
                    return Box2.FromDimensions(0, 0, bounds.Width, bounds.Height);
                }
                return base.LocalAABB;
            }
        }

        public override void Render(Vector2 topLeft, Vector2 bottomRight)
        {
            if (IsCurrentlyWorn && currentSprite == baseSprite)
            {
                base.Render(topLeft, bottomRight);
                return;
            }
            else if (IsCurrentlyCarried && currentSprite != CarriedSprite)
            {
                SetSprite(CarriedSprite);
                base.Render(topLeft, bottomRight);
                return;
            }

            //Render slaves beneath
            IEnumerable<SpriteComponent> renderablesBeneath = from SpriteComponent c in slaves
                                                                  //FIXTHIS
                                                              orderby c.DrawDepth ascending
                                                              where c.DrawDepth < DrawDepth
                                                              select c;

            foreach (SpriteComponent component in renderablesBeneath.ToList())
            {
                component.Render(topLeft, bottomRight);
            }

            //Render this sprite
            if (!visible) return;
            if (NotWornSprite == null) return;

            Sprite spriteToRender = NotWornSprite;
            var bounds = spriteToRender.LocalBounds;

            var renderPos = Owner.GetComponent<ITransformComponent>().WorldPosition * CluwneLib.Camera.PixelsPerMeter;
            spriteToRender.Position = new Vector2(renderPos.X - (bounds.Width / 2),
                                                  renderPos.Y - (bounds.Height / 2));

            if (Owner.GetComponent<ITransformComponent>().WorldPosition.X + bounds.Left + bounds.Width < topLeft.X
                || Owner.GetComponent<ITransformComponent>().WorldPosition.X > bottomRight.X
                || Owner.GetComponent<ITransformComponent>().WorldPosition.Y + bounds.Top + bounds.Height < topLeft.Y
                || Owner.GetComponent<ITransformComponent>().WorldPosition.Y > bottomRight.Y)
                return;

            spriteToRender.Scale = new Vector2(HorizontalFlip ? -1 : 1, 1);
            spriteToRender.Draw();

            //Render slaves above
            IEnumerable<SpriteComponent> renderablesAbove = from SpriteComponent c in slaves
                                                                //FIXTHIS
                                                            orderby c.DrawDepth ascending
                                                            where c.DrawDepth >= DrawDepth
                                                            select c;

            foreach (SpriteComponent component in renderablesAbove.ToList())
            {
                component.Render(topLeft, bottomRight);
            }
        }
    }
}
*/
