using System;
using Robust.Shared.Animations;

namespace Robust.Client.Animations
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
    }
}
