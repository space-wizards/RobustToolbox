using System;
using JetBrains.Annotations;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.Animations
{
    [UsedImplicitly]
    public sealed class AnimationTrackComponentProperty : AnimationTrackProperty
    {
        public Type? ComponentType { get; set; }
        public string? Property { get; set; }

        protected override void ApplyProperty(object context, object value)
        {
            if (Property == null || ComponentType == null)
            {
                throw new InvalidOperationException("Must set parameters to non-null values.");
            }

            var entity = (IEntity) context;
            var component = IoCManager.Resolve<IEntityManager>().GetComponent(entity, ComponentType);

            if (component is IAnimationProperties properties)
            {
                properties.SetAnimatableProperty(Property, value);
            }
            else
            {
                AnimationHelper.SetAnimatableProperty(component, Property, value);
            }
        }
    }
}
