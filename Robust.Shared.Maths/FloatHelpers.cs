using System;
using System.Collections.Generic;
using System.Text;

namespace Robust.Shared.Maths
{
    /// <summary>
    /// Helper functions for floating point math.
    /// </summary>
    public static class FloatHelpers
    {
        /// <summary>
        /// Determines whether the specified value is finite (zero, subnormal, or normal).
        /// </summary>
        /// <param name="val">A single precision floating-point number.</param>
        /// <returns><see langword ="true" /> if the value is finite, <see langword ="false" /> otherwise.</returns>
        public static bool IsFinite(this float val)
        {
            return float.IsFinite(val);
        }
    }
}
