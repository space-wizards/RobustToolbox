using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using SS13_Shared.GO;

namespace GameObject
{
    public interface IComponent
    {
        ComponentFamily Family { get; }
        IEntity Owner { get; set; }

        /// <summary>
        /// Called when the component is removed from an entity.
        /// Shuts down the component
        /// </summary>
        void OnRemove();

        /// <summary>
        /// Called when the component gets added to an entity. 
        /// </summary>
        /// <param name="owner"></param>
        void OnAdd(IEntity owner);

        /// <summary>
        /// Base method to shut down the component. 
        /// </summary>
        void Shutdown();

        /// <summary>
        /// This allows setting of the component's parameters once it is instantiated.
        /// This should basically be overridden by every inheriting component, as parameters will be different
        /// across the board.
        /// </summary>
        /// <param name="parameter">ComponentParameter object describing the parameter and the value</param>
        void SetParameter(ComponentParameter parameter);

        void HandleExtendedParameters(XElement extendedParameters);
    }

    public class Component : IComponent
    {
        public virtual IEntity Owner { get; set; }
        public ComponentFamily Family { get; protected set; }

        public Component()
        {
            Family = ComponentFamily.Generic;
        }

        /// <summary>
        /// Called when the component is removed from an entity.
        /// Shuts down the component
        /// </summary>
        public virtual void OnRemove()
        {
            Owner = null;
            Shutdown();
        }

        /// <summary>
        /// Called when the component gets added to an entity. 
        /// </summary>
        /// <param name="owner"></param>
        public virtual void OnAdd(IEntity owner)
        {
            Owner = owner;
        }

        /// <summary>
        /// Base method to shut down the component. 
        /// </summary>
        public virtual void Shutdown()
        {
        }

        /// <summary>
        /// This allows setting of the component's parameters once it is instantiated.
        /// This should basically be overridden by every inheriting component, as parameters will be different
        /// across the board.
        /// </summary>
        /// <param name="parameter">ComponentParameter object describing the parameter and the value</param>
        public virtual void SetParameter(ComponentParameter parameter)
        {
            switch (parameter.MemberName)
            {
                case "ExtendedParameters":
                    HandleExtendedParameters(parameter.GetValue<XElement>());
                    break;
            }
        }

        public virtual void HandleExtendedParameters(XElement extendedParameters)
        {

        }
    }
}
