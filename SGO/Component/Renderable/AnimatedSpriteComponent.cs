using System;
using System.Collections.Generic;
using GameObject;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Renderable;
using ServerInterfaces.GOC;

namespace SGO
{
    public class AnimatedSpriteComponent : Component, IRenderableComponent
    {
        protected IRenderableComponent master;
        protected List<IRenderableComponent> slaves;
        public string Name;
        private string _currentAnimation;
        public string CurrentAnimation
        {
            get
            {
                if(master != null && master.GetType() == typeof(AnimatedSpriteComponent))
                {
                    return ((AnimatedSpriteComponent)master).CurrentAnimation;
                }
                return _currentAnimation;
            }
            set { _currentAnimation = value; }
        }

        private bool _loop = true;
        public bool Loop
        {
            get
            {
                if (master != null && master.GetType() == typeof(AnimatedSpriteComponent))
                {
                    return ((AnimatedSpriteComponent)master).Loop;
                }
                return _loop;
            }    
            set { _loop = value; }
        }
        public DrawDepth DrawDepth = DrawDepth.FloorTiles;
        public bool Visible = true;


        public AnimatedSpriteComponent()
        {
            Family = ComponentFamily.Renderable;
            slaves = new List<IRenderableComponent>();
        }

        public override ComponentState GetComponentState()
        {
            var masterUid = master != null ? (int?)master.Owner.Uid : null;
            return new AnimatedSpriteComponentState(Visible, DrawDepth, Name, CurrentAnimation, Loop, masterUid);
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

        public bool IsSlaved()
        {
            return master != null;
        }

        public void SetMaster(Entity m)
        {
            if (!m.HasComponent(ComponentFamily.Renderable))
                return;
            var mastercompo = m.GetComponent<IRenderableComponent>(ComponentFamily.Renderable);
            //If there's no sprite component, then FUCK IT
            if (mastercompo == null)
                return;

            // lets get gay together and do some shit like in that stupid book 50 shades of gay
            // “His pointer finger circled my puckered love cave. “Are you ready for this?” he mewled, smirking at me like a mother hamster about to eat her three-legged young.”
            mastercompo.AddSlave(this);
            master = mastercompo;
        }

        public void UnsetMaster()
        {
            if (master == null)
                return;
            master.RemoveSlave(this);
            master = null;
        }

        public void AddSlave(IRenderableComponent slavecompo)
        {
            slaves.Add(slavecompo);
        }

        public void RemoveSlave(IRenderableComponent slavecompo)
        {
            if (slaves.Contains(slavecompo))
                slaves.Remove(slavecompo);
        }
    }
}
