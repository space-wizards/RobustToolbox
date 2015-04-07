using GorgonLibrary;
using GorgonLibrary.Graphics;
using SS14.Client.ClientWindow;
using SS14.Client.Interfaces.Resource;
using SS14.Shared;
using SS14.Shared.GO;
using SS14.Shared.GO.Component.Renderable;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Client.GameObjects
{
    public class WearableAnimatedSpriteComponent : AnimatedSpriteComponent
    {
        public bool IsCurrentlyWorn;
        public Sprite NotWornSprite;

        public bool IsCurrentlyCarried;
        public string CarriedSprite;

        public override Type StateType
        {
            get { return typeof(WearableAnimatedSpriteComponentState); }
        }
        
        public override void HandleComponentState(dynamic state)
        {
            base.HandleComponentState((WearableAnimatedSpriteComponentState)state);
            IsCurrentlyWorn = state.IsCurrentlyWorn;
        }

        public void SetNotWornSprite(string spritename)
        {
            NotWornSprite = IoCManager.Resolve<IResourceManager>().GetSprite(spritename);
        }

        public void SetCarriedSprite(string spritename)
        {
            CarriedSprite = spritename;
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);
            switch (parameter.MemberName)
            {
                case "notWornSprite":
                    SetNotWornSprite(parameter.GetValue<string>());
                    break;
                case "carriedSprite":
                    SetCarriedSprite(parameter.GetValue<string>());
                    break;
            }
        }

        public override void Render(Vector2D topLeft, Vector2D bottomRight)
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
            
            Vector2D renderPos =
                ClientWindowData.Singleton.WorldToScreen(
                    Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);
            spriteToRender.SetPosition(renderPos.X - (spriteToRender.AABB.Width / 2),
                               renderPos.Y - (spriteToRender.AABB.Height / 2));

            if (Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.X + spriteToRender.AABB.Right <
                topLeft.X
                || Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.X > bottomRight.X
                ||
                Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y +
                spriteToRender.AABB.Bottom < topLeft.Y
                || Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y > bottomRight.Y)
                return;

            spriteToRender.HorizontalFlip = flip;
            spriteToRender.Draw();
            spriteToRender.HorizontalFlip = false;

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


            //Draw AABB
            var aabb = AABB;
            //Gorgon.CurrentRenderTarget.Rectangle(renderPos.X - aabb.Width / 2, renderPos.Y - aabb.Height / 2, aabb.Width, aabb.Height, Color.Lime);
        }

    }
}
