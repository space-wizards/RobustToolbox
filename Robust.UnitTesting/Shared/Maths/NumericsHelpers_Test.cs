using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Intrinsics.X86;
using NUnit.Framework;
using Robust.Shared.Maths;
using Microsoft.DotNet.RemoteExecutor;
using System.Runtime.Intrinsics.Arm;

namespace Robust.UnitTesting.Shared.Maths
{
    [Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
    [TestFixture]
    [TestOf(typeof(NumericsHelpers))]
    public sealed class NumericsHelpers_Test
    {
        #region Utils

        private static RemoteInvokeOptions GetInvokeOptions(bool avxEnabled = false)
        {
            var processStartInfo = new ProcessStartInfo();

            processStartInfo.Environment[NumericsHelpers.AvxEnvironmentVariable] = avxEnabled.ToString();

            return new RemoteInvokeOptions() { StartInfo = processStartInfo };
        }

        [Flags]
        enum Intrinsics : byte
        {
            None = 0,
            Sse = 1 << 1,
            Sse2 = 1 << 2,
            Sse3 = 1 << 3,
            Avx = 1 << 4,
            Avx2 = 1 << 5,

            AdvSimd = 1 << 6,
            AdvSimdArm64 = 1 << 7,

            AllX86 = Sse | Sse2 | Sse3 | Avx | Avx2,
            AllArm = AdvSimd | AdvSimdArm64,
        }

        private static bool ValidComputer(Intrinsics flags = Intrinsics.None)
        {
            if (!RemoteExecutor.IsSupported)
                return false;

            if (flags == Intrinsics.None)
                return true;

            // I realize we could do a bitwise AND operation here, but this isn't really written for performance.
            if (flags.HasFlag(Intrinsics.Sse) && !Sse.IsSupported)
                return false;

            if (flags.HasFlag(Intrinsics.Sse2) && !Sse2.IsSupported)
                return false;

            if (flags.HasFlag(Intrinsics.Sse3) && !Sse3.IsSupported)
                return false;

            if (flags.HasFlag(Intrinsics.Avx) && !Avx.IsSupported)
                return false;

            if (flags.HasFlag(Intrinsics.Avx2) && !Avx2.IsSupported)
                return false;

            if (flags.HasFlag(Intrinsics.AdvSimd) && !AdvSimd.IsSupported)
                return false;

            if (flags.HasFlag(Intrinsics.AdvSimdArm64) && !AdvSimd.IsSupported)
                return false;

            return true;
        }

        private void EqualsApprox(ReadOnlySpan<float> a, ReadOnlySpan<float> b, double tolerance = .00001)
        {
            Assert.That(a.Length, Is.EqualTo(b.Length));

            for (var i = 0; i < a.Length; i++)
            {
                Assert.That(b[i], Is.Approximately(a[i], tolerance));
            }
        }

        #endregion

        /*
        [Test]
        public void EnvironmentVariablesWorkAvx()
        {
            // The next one is only valid if the computer supports AVX.
            if (!ValidComputer(Intrinsics.Avx2))
                Assert.Ignore();

            // Enabling NumericsHelper and enabling AVX.
            RemoteExecutor.Invoke(() => { Assert.That(NumericsHelpers.Vector256Enabled, Is.True); },
                GetInvokeOptions(true)).Dispose();
        }
        */

        #region Multiply

        private static readonly float[] MultiplyA =
        {
            1f,
            0f,
            1f,
            2f,
            10f,
            0.1f,
            1234f,
            678.234f,
        };

        private static readonly float[] MultiplyB =
        {
            1f,
            0f,
            0f,
            2f,
            10f,
            1f,
            4321f,
            567.123f,
        };

        private static readonly float[] MultiplyResult =
        {
            1f,
            0f,
            0f,
            4f,
            100f,
            0.1f,
            5332114f,
            384642.101f,
        };

        [Test]
        public void MultiplyScalar()
        {
            Span<float> s = stackalloc float[MultiplyResult.Length];

            NumericsHelpers.MultiplyScalar(MultiplyA, MultiplyB, s, 0, MultiplyA.Length);

            EqualsApprox(MultiplyResult, s);
        }

        [Test]
        public void Multiply128()
        {
            Span<float> s = stackalloc float[MultiplyResult.Length];

            NumericsHelpers.Multiply128(MultiplyA, MultiplyB, s);

            EqualsApprox(MultiplyResult, s);
        }

        [Test]
        public void Multiply256()
        {
            Span<float> s = stackalloc float[MultiplyResult.Length];

            NumericsHelpers.Multiply256(MultiplyA, MultiplyB, s);

            EqualsApprox(MultiplyResult, s);
        }

        #endregion

        #region MultiplyByScalar

        private static readonly float[] MultiplyByScalarA =
        {
            1f,
            0f,
            0.01f,
            2f,
            10f,
            0.1f,
            1234f,
            678.234f,
        };

        private const float MultiplyByScalarB = 50f;

        private static readonly float[] MultiplyByScalarResult =
        {
            50f,
            0f,
            0.5f,
            100f,
            500f,
            5f,
            61700f,
            33911.7f,
        };


        [Test]
        public void MultiplyByScalarScalar()
        {
            Span<float> s = stackalloc float[MultiplyByScalarResult.Length];

            NumericsHelpers.MultiplyScalar(MultiplyByScalarA, MultiplyByScalarB, s, 0, MultiplyByScalarA.Length);

            EqualsApprox(MultiplyByScalarResult, s);
        }

        [Test]
        public void MultiplyByScalar128()
        {
            Span<float> s = stackalloc float[MultiplyByScalarResult.Length];

            NumericsHelpers.Multiply128(MultiplyByScalarA, MultiplyByScalarB, s);

            EqualsApprox(MultiplyByScalarResult, s);
        }

        [Test]
        public void MultiplyByScalar256()
        {
            Span<float> s = stackalloc float[MultiplyByScalarResult.Length];

            NumericsHelpers.Multiply256(MultiplyByScalarA, MultiplyByScalarB, s);

            EqualsApprox(MultiplyByScalarResult, s);
        }

        #endregion

        #region Divide

        private static readonly float[] DivideA =
        {
            1f,
            0f,
            1f,
            2f,
            100f,
            0.1f,
            1234f,
            678.234f,
        };

        private static readonly float[] DivideB =
        {
            1f,
            1f,
            2f,
            2f,
            10f,
            1f,
            4321f,
            567.123f,
        };

        private static readonly float[] DivideResult =
        {
            1f,
            0f,
            0.5f,
            1f,
            10f,
            0.1f,
            0.285582041f,
            1.19592046f,
        };

        [Test]
        public void DivideScalar()
        {
            Span<float> s = stackalloc float[DivideResult.Length];

            NumericsHelpers.DivideScalar(DivideA, DivideB, s, 0, DivideA.Length);

            EqualsApprox(DivideResult, s);
        }

        [Test]
        public void Divide128()
        {
            Span<float> s = stackalloc float[DivideResult.Length];

            NumericsHelpers.Divide128(DivideA, DivideB, s);

            EqualsApprox(DivideResult, s);
        }

        [Test]
        public void Divide256()
        {
            Span<float> s = stackalloc float[DivideResult.Length];

            NumericsHelpers.Divide256(DivideA, DivideB, s);

            EqualsApprox(DivideResult, s);
        }

        #endregion

        #region DivideByScalar

        private static readonly float[] DivideByScalarA =
        {
            1f,
            0f,
            10000f,
            2000f,
            1234f,
            9999f,
            12340f,
            678.234f,
        };

        private const float DivideByScalarB = 1234f;

        private static readonly float[] DivideByScalarResult =
        {
            0.000810372771f,
            0f,
            8.10372771f,
            1.62074554f,
            1f,
            8.10291734f,
            10f,
            0.549622366f,
        };

        [Test]
        public void DivideByScalarScalar()
        {
            Span<float> s = stackalloc float[DivideByScalarResult.Length];

            NumericsHelpers.DivideScalar(DivideByScalarA, DivideByScalarB, s, 0, DivideByScalarA.Length);

            EqualsApprox(DivideByScalarResult, s);
        }

        [Test]
        public void DivideByScalar128()
        {
            Span<float> s = stackalloc float[DivideByScalarResult.Length];

            NumericsHelpers.Divide128(DivideByScalarA, DivideByScalarB, s);

            EqualsApprox(DivideByScalarResult, s);
        }

        [Test]
        public void DivideByScalar256()
        {
            Span<float> s = stackalloc float[DivideByScalarResult.Length];

            NumericsHelpers.Divide256(DivideByScalarA, DivideByScalarB, s);

            EqualsApprox(DivideByScalarResult, s);
        }

        #endregion

        #region Add

        private static readonly float[] AddA =
        {
            1f,
            0f,
            1f,
            2f,
            100f,
            0.1f,
            1234f,
            678.234f,
            1f,
        };

        private static readonly float[] AddB =
        {
            1f,
            1f,
            2f,
            2f,
            10f,
            1f,
            4321f,
            567.123f,
            -1f,
        };

        private static readonly float[] AddResult =
        {
            2f,
            1f,
            3f,
            4f,
            110f,
            1.1f,
            5555f,
            1245.357f,
            0f,
        };

        [Test]
        public void AddScalar()
        {
            Span<float> s = stackalloc float[AddResult.Length];

            NumericsHelpers.AddScalar(AddA, AddB, s, 0, AddA.Length);

            EqualsApprox(AddResult, s);
        }

        [Test]
        public void Add128()
        {
            Span<float> s = stackalloc float[AddResult.Length];

            NumericsHelpers.Add128(AddA, AddB, s);

            EqualsApprox(AddResult, s);
        }

        [Test]
        public void Add256()
        {
            Span<float> s = stackalloc float[AddResult.Length];

            NumericsHelpers.Add256(AddA, AddB, s);

            EqualsApprox(AddResult, s);
        }

        #endregion

        #region AddByScalar

        private static readonly float[] AddByScalarA =
        {
            1f,
            0f,
            15f,
            2f,
            100f,
            0.1f,
            1234f,
            678.234f,
        };

        private const float AddByScalarB = 100f;

        private static readonly float[] AddByScalarResult =
        {
            101f,
            100f,
            115f,
            102f,
            200f,
            100.1f,
            1334f,
            778.234f,
        };

        [Test]
        public void AddByScalarScalar()
        {
            Span<float> s = stackalloc float[AddByScalarResult.Length];

            NumericsHelpers.AddScalar(AddByScalarA, AddByScalarB, s, 0, AddByScalarA.Length);

            EqualsApprox(AddByScalarResult, s);
        }

        [Test]
        public void AddByScalar128()
        {
            Span<float> s = stackalloc float[AddByScalarResult.Length];

            NumericsHelpers.Add128(AddByScalarA, AddByScalarB, s);

            EqualsApprox(AddByScalarResult, s);
        }

        [Test]
        public void AddByScalar256()
        {
            Span<float> s = stackalloc float[AddByScalarResult.Length];

            NumericsHelpers.Add256(AddByScalarA, AddByScalarB, s);

            EqualsApprox(AddByScalarResult, s);
        }

        #endregion

        #region HorizontalAdd

        private static readonly float[] HorizontalAddA =
        {
            1f,
            0f,
            1f,
            2f,
            100f,
            0.1f,
            1234f,
            678.234f,
            10f,
            1f,
            0f,
            1f,
            2f,
            100f,
            0.1f,
            1234f,
            678.234f,
        };

        private const float HorizontalAddResult = 4042.668f;

        [Test]
        public void HorizontalAddScalar()
        {
            var added = NumericsHelpers.HorizontalAddScalar(HorizontalAddA, 0, HorizontalAddA.Length);

            Assert.That(added, Is.Approximately(HorizontalAddResult));
        }

        [Test]
        public void HorizontalAdd128()
        {
            var added = NumericsHelpers.HorizontalAdd128(HorizontalAddA);

            Assert.That(added, Is.Approximately(HorizontalAddResult));
        }

        [Test]
        public void HorizontalAdd256()
        {
            var added = NumericsHelpers.HorizontalAdd256(HorizontalAddA);

            Assert.That(added, Is.Approximately(HorizontalAddResult));
        }

        #endregion

        #region Sub

        private static readonly float[] SubA =
        {
            1f,
            0f,
            1f,
            2f,
            100f,
            0.1f,
            1234f,
            678.234f,
        };

        private static readonly float[] SubB =
        {
            1f,
            1f,
            2f,
            2f,
            10f,
            1f,
            4321f,
            567.123f,
        };

        private static readonly float[] SubResult =
        {
            0f,
            -1f,
            -1f,
            0f,
            90f,
            -0.9f,
            -3087f,
            111.11100f,
        };

        [Test]
        public void SubScalar()
        {
            Span<float> s = stackalloc float[SubResult.Length];

            NumericsHelpers.SubScalar(SubA, SubB, s, 0, SubA.Length);

            EqualsApprox(SubResult, s);
        }

        [Test]
        public void Sub128()
        {
            Span<float> s = stackalloc float[SubResult.Length];

            NumericsHelpers.Sub128(SubA, SubB, s);

            EqualsApprox(SubResult, s);
        }

        [Test]
        public void Sub256()
        {
            Span<float> s = stackalloc float[SubResult.Length];

            NumericsHelpers.Sub256(SubA, SubB, s);

            EqualsApprox(SubResult, s);
        }

        #endregion

        #region SubByScalar

        private static readonly float[] SubByScalarA =
        {
            1f,
            0f,
            15f,
            2f,
            100f,
            0.1f,
            1234f,
            678.234f,
        };

        private const float SubByScalarB = 100f;

        private static readonly float[] SubByScalarResult =
        {
            -99f,
            -100f,
            -85f,
            -98f,
            0f,
            -99.9f,
            1134f,
            578.234f,
        };

        [Test]
        public void SubByScalarScalar()
        {
            Span<float> s = stackalloc float[SubByScalarResult.Length];

            NumericsHelpers.SubScalar(SubByScalarA, SubByScalarB, s, 0, SubByScalarA.Length);

            EqualsApprox(SubByScalarResult, s);
        }

        [Test]
        public void SubByScalar128()
        {
            Span<float> s = stackalloc float[SubByScalarResult.Length];

            NumericsHelpers.Sub128(SubByScalarA, SubByScalarB, s);

            EqualsApprox(SubByScalarResult, s);
        }

        [Test]
        public void SubByScalar256()
        {
            Span<float> s = stackalloc float[SubByScalarResult.Length];

            NumericsHelpers.Sub256(SubByScalarA, SubByScalarB, s);

            EqualsApprox(SubByScalarResult, s);
        }

        #endregion

        #region Abs

        private static readonly float[] AbsA =
        {
            -1f,
            0f,
            -0f,
            -15f,
            -2f,
            100f,
            0.1f,
            -1234f,
            -678.234f,
        };

        private static readonly float[] AbsResult =
        {
            1f,
            0f,
            0f,
            15f,
            2f,
            100f,
            0.1f,
            1234f,
            678.234f,
        };

        [Test]
        public void AbsScalar()
        {
            Span<float> s = stackalloc float[AbsResult.Length];

            NumericsHelpers.AbsScalar(AbsA, s, 0, AbsA.Length);

            EqualsApprox(AbsResult, s);
        }

        [Test]
        public void Abs128()
        {
            Span<float> s = stackalloc float[AbsResult.Length];

            NumericsHelpers.Abs128(AbsA, s);

            EqualsApprox(AbsResult, s);
        }

        [Test]
        public void Abs256()
        {
            Span<float> s = stackalloc float[AbsResult.Length];

            NumericsHelpers.Abs256(AbsA, s);

            EqualsApprox(AbsResult, s);
        }

        #endregion

        #region Min

        private static readonly float[] MinA =
        {
            1f,
            0f,
            1f,
            2f,
            100f,
            0.1f,
            1234f,
            678.234f,
        };

        private static readonly float[] MinB =
        {
            1f,
            1f,
            2f,
            2f,
            10f,
            1f,
            4321f,
            567.123f,
        };

        private static readonly float[] MinResult =
        {
            1f,
            0f,
            1f,
            2f,
            10f,
            0.1f,
            1234f,
            567.123f,
        };

        [Test]
        public void MinScalar()
        {
            Span<float> s = stackalloc float[MinResult.Length];

            NumericsHelpers.MinScalar(MinA, MinB, s, 0, MinA.Length);

            EqualsApprox(MinResult, s);
        }

        [Test]
        public void Min128()
        {
            Span<float> s = stackalloc float[MinResult.Length];

            NumericsHelpers.Min128(MinA, MinB, s);

            EqualsApprox(MinResult, s);
        }

        [Test]
        public void Min256()
        {
            Span<float> s = stackalloc float[MinResult.Length];

            NumericsHelpers.Min256(MinA, MinB, s);

            EqualsApprox(MinResult, s);
        }

        #endregion

        #region MinByScalar

        private static readonly float[] MinByScalarA =
        {
            1f,
            0f,
            1f,
            2f,
            100f,
            0.1f,
            1234f,
            678.234f,
            0.05f,
            0.5f,
            -12.5f,
        };

        private const float MinByScalarB = 1f;

        private static readonly float[] MinByScalarR =
        {
            1f,
            0f,
            1f,
            1f,
            1f,
            0.1f,
            1f,
            1f,
            0.05f,
            0.5f,
            -12.5f,
        };

        [Test]
        public void MinByScalarScalar()
        {
            Span<float> s = stackalloc float[MinByScalarR.Length];

            NumericsHelpers.MinScalar(MinByScalarA, MinByScalarB, s, 0, MinByScalarA.Length);

            EqualsApprox(MinByScalarR, s);
        }

        [Test]
        public void MinByScalar128()
        {
            Span<float> s = stackalloc float[MinByScalarR.Length];

            NumericsHelpers.Min128(MinByScalarA, MinByScalarB, s);

            EqualsApprox(MinByScalarR, s);
        }

        [Test]
        public void MinByScalar256()
        {
            Span<float> s = stackalloc float[MinByScalarR.Length];

            NumericsHelpers.Min256(MinByScalarA, MinByScalarB, s);

            EqualsApprox(MinByScalarR, s);
        }

        #endregion

        #region Max

        private static readonly float[] MaxA =
        {
            1f,
            0f,
            1f,
            2f,
            100f,
            0.1f,
            1234f,
            678.234f,
        };

        private static readonly float[] MaxB =
        {
            1f,
            1f,
            2f,
            2f,
            10f,
            1f,
            4321f,
            567.123f,
        };

        private static readonly float[] MaxResult =
        {
            1f,
            1f,
            2f,
            2f,
            100f,
            1f,
            4321f,
            678.234f,
        };

        [Test]
        public void MaxScalar()
        {
            Span<float> s = stackalloc float[MaxResult.Length];

            NumericsHelpers.MaxScalar(MaxA, MaxB, s, 0, MaxA.Length);

            EqualsApprox(MaxResult, s);
        }

        [Test]
        public void Max128()
        {
            Span<float> s = stackalloc float[MaxResult.Length];

            NumericsHelpers.Max128(MaxA, MaxB, s);

            EqualsApprox(MaxResult, s);
        }

        [Test]
        public void Max256()
        {
            Span<float> s = stackalloc float[MaxResult.Length];

            NumericsHelpers.Max256(MaxA, MaxB, s);

            EqualsApprox(MaxResult, s);
        }

        #endregion

        #region MaxByScalar

        private static readonly float[] MaxByScalarA =
        {
            1f,
            0f,
            1f,
            200f,
            100f,
            0.1f,
            1234f,
            678.234f,
        };

        private const float MaxByScalarB = 100f;

        private static readonly float[] MaxByScalarResult =
        {
            100f,
            100f,
            100f,
            200f,
            100f,
            100f,
            1234f,
            678.234f,
        };

        [Test]
        public void MaxByScalarScalar()
        {
            Span<float> s = stackalloc float[MaxByScalarResult.Length];

            NumericsHelpers.MaxScalar(MaxByScalarA, MaxByScalarB, s, 0, MaxByScalarA.Length);

            EqualsApprox(MaxByScalarResult, s);
        }

        [Test]
        public void MaxByScalar128()
        {
            Span<float> s = stackalloc float[MaxByScalarResult.Length];

            NumericsHelpers.Max128(MaxByScalarA, MaxByScalarB, s);

            EqualsApprox(MaxByScalarResult, s);
        }

        [Test]
        public void MaxByScalar256()
        {
            Span<float> s = stackalloc float[MaxByScalarResult.Length];

            NumericsHelpers.Max256(MaxByScalarA, MaxByScalarB, s);

            EqualsApprox(MaxByScalarResult, s);
        }

        #endregion
    }
}
