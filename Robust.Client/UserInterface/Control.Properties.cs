using System;
using System.Collections.Generic;
using System.Linq;

namespace Robust.Client.UserInterface
{
    public partial class Control
    {
        private Dictionary<AttachedProperty, object?>? _attachedProperties;

        /// <summary>
        /// Gets the value of an attached property on this control.
        /// </summary>
        /// <param name="property">The attached property to get the value of.</param>
        /// <returns>
        /// The property's value for this object,
        /// or the property's default value if it has not been set on this control yet.
        /// </returns>
        public object? GetValue(AttachedProperty property)
        {
            if (_attachedProperties == null)
            {
                return property.DefaultValue;
            }

            if (!_attachedProperties.TryGetValue(property, out var value))
            {
                return property.DefaultValue;
            }

            return value;
        }

        public T GetValue<T>(AttachedProperty<T> property)
        {
            if (_attachedProperties == null)
            {
                return property.DefaultValue;
            }

            if (!_attachedProperties.TryGetValue(property, out var value))
            {
                return property.DefaultValue;
            }

            return (T) value!;
        }

        /// <summary>
        /// Gets the value of an attached property on this control.
        /// </summary>
        /// <param name="property">The attached property to get the value of.</param>
        /// <typeparam name="T">The type to cast the property value to, for convenience.</typeparam>
        /// <returns>
        /// The property's value for this object,
        /// or the property's default value if it has not been set on this control yet.
        /// </returns>
        /// <exception cref="InvalidCastException">
        /// Thrown if the property value is not of type <typeparamref name="T"/>
        /// </exception>
        public T GetValue<T>(AttachedProperty property)
        {
            return (T) GetValue(property)!;
        }

        /// <summary>
        /// Sets the value of an attached property on this control.
        /// </summary>
        /// <remarks>
        /// If possible, it is recommended to use <see cref="SetValue{T}"/> instead.
        /// </remarks>
        /// <param name="property">The attached property to set.</param>
        /// <param name="value">The new value.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the property type is a non-nullable value type, but <paramref name="value"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if the type of <paramref name="value"/> is not assignable to the property's type.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if the property has a validation function, and the validation function failed.
        /// </exception>
        public void SetValue(AttachedProperty property, object? value)
        {
            if (_attachedProperties == null)
            {
                _attachedProperties = new Dictionary<AttachedProperty, object?>();
            }

            // Verify that the value can be assigned to the property.
            if (value == null)
            {
                if (property.PropertyType.IsValueType &&
                    // Nullable<T> actually boxes as null. Keep that in mind.
                    Nullable.GetUnderlyingType(property.PropertyType) == null)
                {
                    throw new ArgumentNullException(nameof(value),
                        "Property is a value type, but null was passed as value.");
                }
            }
            else
            {
                if (!property.PropertyType.IsInstanceOfType(value))
                {
                    throw new ArgumentException("Value is of wrong type for property.", nameof(value));
                }
            }

            if (property.Validate != null && !property.Validate(value))
            {
                throw new ArgumentException("Value is not valid for this property.", nameof(value));
            }

            if (!_attachedProperties.TryGetValue(property, out var oldValue))
            {
                oldValue = property.DefaultValue;
            }

            var changed = new AttachedPropertyChangedEventArgs(value, oldValue);

            _attachedProperties[property] = value;

            property.Changed?.Invoke(this, changed);
        }

        public void SetValue<T>(AttachedProperty<T> property, T value)
        {
            _attachedProperties ??= new Dictionary<AttachedProperty, object?>();

            if (property.Validate != null && !property.Validate(value))
            {
                throw new ArgumentException("Value is not valid for this property.", nameof(value));
            }

            T oldValue;
            if (!_attachedProperties.TryGetValue(property, out var oldValueBoxed))
            {
                oldValue = property.DefaultValue;
            }
            else
            {
                oldValue = (T) oldValueBoxed!;
            }

            var changed = new AttachedPropertyChangedEventArgs<T>(value, oldValue);

            _attachedProperties[property] = value;

            property.Changed?.Invoke(this, changed);
        }

        // Using generics simplifies the type check A LOT, so this is preferred.
        /// <summary>
        /// Sets the value of an attached property on this control.
        /// </summary>
        /// <param name="property">The attached property to set.</param>
        /// <param name="value">The new value.</param>
        /// <typeparam name="T">
        /// The type of the value being assigned.
        /// </typeparam>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the type of <paramref name="value"/> is not assignable to the property's type.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if the property has a validation function, and the validation function failed.
        /// </exception>
        public void SetValue<T>(AttachedProperty property, T value)
        {
            if (_attachedProperties == null)
            {
                _attachedProperties = new Dictionary<AttachedProperty, object?>();
            }

            if (!property.PropertyType.IsAssignableFrom(typeof(T)))
            {
                throw new InvalidOperationException("Property is of the wrong type.");
            }

            if (typeof(T) == typeof(object))
            {
                SetValue(property, (object?) value);
                return;
            }

            if (property.Validate != null && !property.Validate(value))
            {
                throw new ArgumentException("Value is not valid for this property.", nameof(value));
            }

            if (!_attachedProperties.TryGetValue(property, out var oldValue))
            {
                oldValue = property.DefaultValue;
            }

            var changed = new AttachedPropertyChangedEventArgs(value, oldValue);

            _attachedProperties[property] = value;

            property.Changed?.Invoke(this, changed);
        }

        public IEnumerable<KeyValuePair<AttachedProperty, object?>> AllAttachedProperties =>
            _attachedProperties ?? Enumerable.Empty<KeyValuePair<AttachedProperty, object?>>();
    }
}
