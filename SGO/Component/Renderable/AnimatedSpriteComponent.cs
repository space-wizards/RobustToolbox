using System;
using GameObject;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Renderable;

namespace SGO
{
    public class AnimatedSpriteComponent : Component
    {

        public string Name;
        public string CurrentAnimation;
        public DrawDepth DrawDepth = DrawDepth.FloorTiles;
        public bool Visible = true;
        public bool Loop = true;

        public AnimatedSpriteComponent()
        {
            Family = ComponentFamily.Renderable;
        }

        public override ComponentState GetComponentState()
        {
            return new AnimatedSpriteComponentState(Visible, DrawDepth, Name, CurrentAnimation, Loop);
        }
        
        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);
            switch (parameter.MemberName)
            {
                case "drawdepth":
                    DrawDepth = (DrawDepth)Enum.Parse(typeof(DrawDepth), parameter.GetValue<string>(), true);
                    break;

                case "sprite":
                    Name = parameter.GetValue<string>();
                    break;
            }
        }

        public void SetAnimationState(string state, bool loop = true)
        {
            CurrentAnimation = state;
            Loop = loop;
        }
    }
}
