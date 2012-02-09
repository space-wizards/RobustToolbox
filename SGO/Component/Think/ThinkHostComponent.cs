using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SGO.Component.Think.ThinkComponent;
using SS13_Shared;

namespace SGO
{
    public class ThinkHostComponent : GameObjectComponent
    {
        private List<IThinkComponent> ThinkComponents = new List<IThinkComponent>();

        public ThinkHostComponent()
        {
            family = SS13_Shared.GO.ComponentFamily.Think;
        }

        public override void RecieveMessage(object sender, SS13_Shared.GO.ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            base.RecieveMessage(sender, type, replies, list);
            switch(type)
            {
                case SS13_Shared.GO.ComponentMessageType.Bumped:
                    OnBump(sender, list);
                    break;
            }
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);

            switch(parameter.MemberName)
            {
                case "LoadThinkComponent":
                    IThinkComponent c = GetThinkComponent((string)parameter.Parameter);
                    if (c == null)
                        break;
                    ThinkComponents.Add(c);
                    break;
            }
        }

        public void OnBump(object sender, params object[] list)
        {
            foreach (IThinkComponent c in ThinkComponents)
            {
                c.OnBump(sender, list);
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (IThinkComponent c in ThinkComponents)
            {
                c.Update(frameTime);
            }
        }

        /// <summary>
        /// Gets a new component instantiated of the specified type.
        /// </summary>
        /// <param name="componentType">type of component to make</param>
        /// <returns>A GameObjectComponent</returns>
        public IThinkComponent GetThinkComponent(string componentTypeName)
        {
            if (componentTypeName == null || componentTypeName == "")
                return null;
            Type t = Type.GetType("SGO.Component.Think.ThinkComponent." + componentTypeName); //Get the type
            if (t == null || t.GetInterface("IThinkComponent") == null)
                return null;

            return (IThinkComponent)Activator.CreateInstance(t); // Return an instance
        }

    }
}
