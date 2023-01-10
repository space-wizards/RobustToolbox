using System;
using System.Runtime.Intrinsics;

namespace Robust.Shared.Maths
{
    public static partial class NumericsHelpers
    {
        #region Constructor & Environment Variables

        // Misnomer due to historical reasons.
        public const string AvxEnvironmentVariable = "ROBUST_NUMERICS_AVX";

        /// <summary>
        ///     Whether AVX is enabled.
        /// </summary>
        public static readonly bool Vector256Enabled;

        static NumericsHelpers()
        {
            var avxEnabled = Environment.GetEnvironmentVariable(AvxEnvironmentVariable);
            Vector256Enabled = Vector256.IsHardwareAccelerated && avxEnabled != null && bool.Parse(avxEnabled);
        }

        #endregion

        #region Utils

        /// <summary>
        ///     Returns whether the specified array length is valid for loading into 256-bit registers.
        /// </summary>
        private static bool LengthValid256Single(int arrayLength)
        {
            return arrayLength >= 8;
        }

        #endregion

    }
}
