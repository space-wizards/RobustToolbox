using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;

namespace Robust.Shared.Maths
{
    public static unsafe partial class NumericsHelpers
    {
        #region Constructor & Environment Variables

        public const string DisabledEnvironmentVariable = "ROBUST_NUMERICS_DISABLED";
        public const string AvxEnvironmentVariable = "ROBUST_NUMERICS_AVX";

        /// <summary>
        ///     Whether to use the hardware-accelerated paths.
        /// </summary>
        public static readonly bool Enabled;

        /// <summary>
        ///     Whether AVX is enabled.
        /// </summary>
        public static readonly bool AvxEnabled;

        static NumericsHelpers()
        {
            var disabled = Environment.GetEnvironmentVariable(DisabledEnvironmentVariable);
            var avxEnabled = Environment.GetEnvironmentVariable(AvxEnvironmentVariable);
            Enabled = disabled == null || !bool.Parse(disabled);
            AvxEnabled = Enabled && Avx.IsSupported && avxEnabled != null && bool.Parse(avxEnabled);
        }

        #endregion

        #region Utils

        /// <summary>
        ///     Returns whether the specified array length is valid for loading into 128-bit registers.
        /// </summary>
        private static bool LengthValid128Single(int arrayLength)
        {
            return arrayLength >= 4;
        }

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
