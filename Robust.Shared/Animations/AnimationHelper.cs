using System;

namespace Robust.Shared.Animations
{
    public static class AnimationHelper
    {
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
