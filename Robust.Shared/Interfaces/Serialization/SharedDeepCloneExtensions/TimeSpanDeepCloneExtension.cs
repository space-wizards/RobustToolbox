using System;

namespace Robust.Shared.Interfaces.Serialization.SharedDeepCloneExtensions
{
    [DeepCloneExtension(typeof(TimeSpan))]
    public class TimeSpanDeepCloneExtension : DeepCloneExtension
    {
        public override object DeepClone(object value)
        {
            var timespan = (TimeSpan) value;
            return TimeSpan.FromTicks(timespan.Ticks);
        }
    }
}
