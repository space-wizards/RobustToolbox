using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.GO;
using SS14.Shared.GO.Component.Renderable;
using SS14.Shared.IoC;
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

        public override void Render(Vector2f topLeft, Vector2f bottomRight)
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
            var bounds = spriteToRender.GetLocalBounds();

            Vector2f renderPos = CluwneLib.WorldToScreen(
                    Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);
            spriteToRender.Position = new SFML.System.Vector2f(renderPos.X - (bounds.Width / 2),
                                                               renderPos.Y - (bounds.Height / 2));

            if (Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.X + bounds.Left + bounds.Width < topLeft.X
                || Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.X > bottomRight.X
                || Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y + bounds.Top + bounds.Height < topLeft.Y
                || Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position.Y > bottomRight.Y)
                return;

            spriteToRender.Scale = new SFML.System.Vector2f(HorizontalFlip ? -1 : 1, 1);
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


            //Draw AABB
            var aabb = AABB;
            CluwneLib.drawRectangle((int)(renderPos.X - aabb.Width / 2),(int)(renderPos.Y - aabb.Height / 2), (int)aabb.Width, (int)aabb.Height, new SFML.Graphics.Color(0, 0, 255));
        }

    }
}
