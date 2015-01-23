using SS14.Server.GameObjects.Think.ThinkComponent;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using System;
using System.Collections.Generic;

namespace SS14.Server.GameObjects
{
    public class ThinkHostComponent : Component
    {
        private readonly List<IThinkComponent> ThinkComponents = new List<IThinkComponent>();

        public ThinkHostComponent()
        {
            Family = ComponentFamily.Think;
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.Bumped:
                    OnBump(sender, list);
                    break;
            }

            return reply;
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);

            switch (parameter.MemberName)
            {
                case "LoadThinkComponent":
                    IThinkComponent c = GetThinkComponent(parameter.GetValue<string>());
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
        /// <returns>A Component</returns>
        public IThinkComponent GetThinkComponent(string componentTypeName)
        {
            if (componentTypeName == null || componentTypeName == "")
                return null;
            Type t = Type.GetType("SGO.Think.ThinkComponent." + componentTypeName); //Get the type
            if (t == null || t.GetInterface("IThinkComponent") == null)
                return null;

            return (IThinkComponent) Activator.CreateInstance(t); // Return an instance
        }
    }
}