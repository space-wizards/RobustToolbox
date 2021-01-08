using System;
using Robust.Client.Animations;
using Robust.Shared.Animations;
using Robust.Shared.Interfaces.Serialization;

namespace Content.Client.Animations
{
    public class AnimationTrackControlProperty : AnimationTrackProperty
    {
        public string? Property { get; set; }

        protected override void ApplyProperty(object context, object value)
        {
            if (Property == null)
            {
                throw new InvalidOperationException("Must set property to change.");
            }

            // TODO: Attached property support?
            AnimationHelper.SetAnimatableProperty(context, Property, value);
        }

        public override IDeepClone DeepClone()
        {
            return new AnimationTrackControlProperty
            {
                Property = IDeepClone.CloneValue(Property),
                InterpolationMode = IDeepClone.CloneValue(InterpolationMode),
                KeyFrames = IDeepClone.CloneValue(KeyFrames)!
            };
        }
    }
}
