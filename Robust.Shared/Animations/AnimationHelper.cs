using System;

namespace Robust.Shared.Animations
{
    public static class AnimationHelper
    {
        /// <summary>
        ///     Sets properties marked with <see cref="AnimatableAttribute"/> on an object.
        /// </summary>
        /// <remarks>
        ///     This does not use <see cref="IAnimationProperties"/>.
        /// </remarks>
        /// <param name="target">The object to set the property on.</param>
        /// <param name="name">The name of the property to set.</param>
        /// <param name="value">The value to set.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown if the property does not exist or does not have <see cref="AnimatableAttribute"/>.
        /// </exception>
        public static void SetAnimatableProperty(object target, string name, object value)
        {
            var property = target.GetType().GetProperty(name);
            if (property == null)
            {
                throw new ArgumentException($"Animatable property with name '{name}' does not exist.");
            }

            if (!Attribute.IsDefined(property, typeof(AnimatableAttribute)))
            {
                throw new ArgumentException($"Animatable property with name '{name}' does not exist.");
            }

            property.SetValue(target, value);
        }

        /// <summary>
        ///     Gets the value of a property marked with <see cref="AnimatableAttribute"/> from an object.
        /// </summary>
        /// <remarks>
        ///     This does not use <see cref="IAnimationProperties"/>.
        /// </remarks>
        /// <param name="target">The object to get the property from.</param>
        /// <param name="name">The name of the property to get.</param>
        /// <returns>The current value of the property.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown if the property does not exist or does not have <see cref="AnimatableAttribute"/>.
        /// </exception>
        public static object? GetAnimatableProperty(object target, string name)
        {
            var property = target.GetType().GetProperty(name);
            if (property == null)
            {
                throw new ArgumentException($"Animatable property with name '{name}' does not exist.");
            }

            if (!Attribute.IsDefined(property, typeof(AnimatableAttribute)))
            {
                throw new ArgumentException($"Animatable property with name '{name}' does not exist.");
            }

            return property.GetValue(target);
        }

        public static void CallAnimatableMethod(object target, string name, object[] arguments)
        {
            var method = target.GetType().GetMethod(name);
            if (method == null)
            {
                throw new ArgumentException($"Animatable method with name '{name}' does not exist.");
            }

            if (!Attribute.IsDefined(method, typeof(AnimatableAttribute)))
            {
                throw new ArgumentException($"Animatable method with name '{name}' does not exist.");
            }

            method.Invoke(target, arguments);
        }
    }
}
