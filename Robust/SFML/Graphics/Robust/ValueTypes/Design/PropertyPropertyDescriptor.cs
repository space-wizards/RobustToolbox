using System;
using System.Linq;
using System.Reflection;

namespace SFML.Graphics.Design
{
    class PropertyPropertyDescriptor : MemberPropertyDescriptor
    {
        readonly PropertyInfo _property;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyPropertyDescriptor"/> class.
        /// </summary>
        /// <param name="property">The property.</param>
        public PropertyPropertyDescriptor(PropertyInfo property) : base(property)
        {
            _property = property;
        }

        /// <summary>
        /// When overridden in a derived class, gets the type of the property.
        /// </summary>
        /// <value></value>
        /// <returns>A <see cref="T:System.Type"/> that represents the type of the property.</returns>
        public override Type PropertyType
        {
            get { return _property.PropertyType; }
        }

        /// <summary>
        /// When overridden in a derived class, gets the current value of the property on a component.
        /// </summary>
        /// <param name="component">The component with the property for which to retrieve the value.</param>
        /// <returns>
        /// The value of a property for a given component.
        /// </returns>
        public override object GetValue(object component)
        {
            return _property.GetValue(component, null);
        }

        /// <summary>
        /// When overridden in a derived class, sets the value of the component to a different value.
        /// </summary>
        /// <param name="component">The component with the property value that is to be set.</param>
        /// <param name="value">The new value.</param>
        public override void SetValue(object component, object value)
        {
            _property.SetValue(component, value, null);
            OnValueChanged(component, EventArgs.Empty);
        }
    }
}