using System;
using JetBrains.Annotations;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;

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

            var entity = (EntityUid) context;
            var entManager = IoCManager.Resolve<IEntityManager>();

            if (!entManager.TryGetComponent(entity, ComponentType, out var component))
            {
                // This gets checked when the animation is first played, but the component may also be removed while the animation plays
                Logger.Error($"Couldn't find component {ComponentType} on {entManager.ToPrettyString(entity)} for animation playback!");
                return;
            }

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
