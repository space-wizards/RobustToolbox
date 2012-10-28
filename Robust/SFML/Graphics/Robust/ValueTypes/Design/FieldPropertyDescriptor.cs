using System;
using System.Linq;
using System.Reflection;

namespace SFML.Graphics.Design
{
    class FieldPropertyDescriptor : MemberPropertyDescriptor
    {
        readonly FieldInfo _field;

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldPropertyDescriptor"/> class.
        /// </summary>
        /// <param name="field">The field.</param>
        public FieldPropertyDescriptor(FieldInfo field) : base(field)
        {
            _field = field;
        }

        /// <summary>
        /// When overridden in a derived class, gets the type of the property.
        /// </summary>
        /// <value></value>
        /// <returns>A <see cref="T:System.Type"/> that represents the type of the property.</returns>
        public override Type PropertyType
        {
            get { return _field.FieldType; }
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
            return _field.GetValue(component);
        }

        /// <summary>
        /// When overridden in a derived class, sets the value of the component to a different value.
        /// </summary>
        /// <param name="component">The component with the property value that is to be set.</param>
        /// <param name="value">The new value.</param>
        public override void SetValue(object component, object value)
        {
            _field.SetValue(component, value);
            OnValueChanged(component, EventArgs.Empty);
        }
    }
}