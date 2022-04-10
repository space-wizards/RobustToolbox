using System;
using Robust.Client.UserInterface.Controls;

namespace Robust.Client.UserInterface
{
    /// <param name="owner">Control on which the property was changed.</param>
    /// <param name="eventArgs"></param>
    public delegate void AttachedPropertyChangedCallback(Control owner, AttachedPropertyChangedEventArgs eventArgs);

    public delegate void AttachedPropertyChangedCallback<T>(Control owner,
        AttachedPropertyChangedEventArgs<T> eventArgs);

    /// <summary>
    ///     An attached property is a property that can be assigned to any control,
    ///     without having to modify the base <see cref="Control" /> class to add it.
    ///     This is useful for storing data for specific controls like <see cref="LayoutContainer" />
    /// </summary>
    [Virtual]
    public class AttachedProperty
    {
        /// <summary>
        ///     The name of the property.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     The type that defines the attached property.
        /// </summary>
        public Type OwningType { get; }

        /// <summary>
        ///     The type of the value stored in the property.
        /// </summary>
        public Type PropertyType { get; }

        /// <summary>
        ///     The default value of the property.
        ///     This is returned if no value is set and <see cref="Control.GetValue"/> is called.
        /// </summary>
        public object? DefaultValue { get; }

        /// <summary>
        ///     An optional validation function.
        ///     If the value to <see cref="Control.SetValue"/> fails this check, an exception will be thrown.
        /// </summary>
        public Func<object?, bool>? Validate { get; }

        /// <summary>
        ///     A callback to run whenever this property changes on a control.
        /// </summary>
        public AttachedPropertyChangedCallback? Changed { get; }

        internal AttachedProperty(string name, Type owningType, Type propertyType,
            object? defaultValue = null,
            Func<object?, bool>? validate = null,
            AttachedPropertyChangedCallback? changed = null)
        {
            Name = name;
            OwningType = owningType;
            PropertyType = propertyType;
            DefaultValue = defaultValue;
            Validate = validate;
            Changed = changed;
        }

        /// <remarks>
        ///     Parameters correspond to properties on this class.
        /// </remarks>
        public static AttachedProperty Create(
            string name, Type owningType, Type propertyType,
            object? defaultValue = null,
            Func<object?, bool>? validate = null,
            AttachedPropertyChangedCallback? changed = null)
        {
            if (propertyType.IsValueType && defaultValue == null)
            {
                // Use activator to create uninitialized version of value type.
                defaultValue = Activator.CreateInstance(propertyType)!;
            }

            return new AttachedProperty(name, owningType, propertyType, defaultValue, validate, changed);
        }
    }

    public sealed class AttachedProperty<T> : AttachedProperty
    {
        public new Func<T, bool>? Validate { get; }

        public new AttachedPropertyChangedCallback<T>? Changed { get; }

        public new T DefaultValue { get; }

        internal AttachedProperty(string name, Type owningType, T defaultValue,
            Func<T, bool>? validate = null, AttachedPropertyChangedCallback<T>? changed = null)
            : base(name, owningType, typeof(T), defaultValue,
                validate != null ? o => validate!((T) o!) : null,
                changed != null
                    ? (o, ev) => changed!(o, new AttachedPropertyChangedEventArgs<T>((T) ev.NewValue!, (T) ev.OldValue!))
                    : null)
        {
            Validate = validate;
            Changed = changed;
            DefaultValue = defaultValue;
        }

        public static AttachedProperty<T> Create(string name, Type owningType,
            T defaultValue = default!,
            Func<T, bool>? validate = null,
            AttachedPropertyChangedCallback<T>? changed = null)
        {
            if (!typeof(T).IsValueType && defaultValue == null)
            {
                throw new ArgumentNullException(nameof(defaultValue),
                    "Got defaultValue that is null for reference type." +
                    "If this is a non-nullable reference type," +
                    "make sure to fill in a default value with the parameter." +
                    "If this is intended to be nullable," +
                    "use the CreateNull() overload (and make sure to set the type nullability correctly!).");
            }

            return new AttachedProperty<T>(name, owningType, defaultValue, validate, changed);
        }

        // TODO: C# 9: use nullable T on the returned attached property here.
        public static AttachedProperty<T> CreateNull(string name, Type owningType,
            T defaultValue = default!,
            Func<T, bool>? validate = null,
            AttachedPropertyChangedCallback<T>? changed = null)
        {
            if (typeof(T).IsValueType)
            {
                throw new ArgumentException("Type must not be a value type. Use regular create for that" +
                                            " (yes, even for nullable value types).");
            }
            return new AttachedProperty<T>(name, owningType, defaultValue, validate, changed);
        }

    }

    /// <summary>
    ///     Event args for when an attached property on a control changes.
    /// </summary>
    public readonly struct AttachedPropertyChangedEventArgs
    {
        public AttachedPropertyChangedEventArgs(object? newValue, object? oldValue)
        {
            NewValue = newValue;
            OldValue = oldValue;
        }

        public object? NewValue { get; }
        public object? OldValue { get; }
    }

    public readonly struct AttachedPropertyChangedEventArgs<T>
    {
        public T NewValue { get; }
        public T OldValue { get; }

        public AttachedPropertyChangedEventArgs(T newValue, T oldValue)
        {
            NewValue = newValue;
            OldValue = oldValue;
        }
    }
}
