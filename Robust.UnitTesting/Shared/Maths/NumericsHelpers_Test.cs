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
    [SuppressMessage("ReSharper", "AccessToStaticMemberViaDerivedType")]
    public sealed class NumericsHelpers_Test
    {
        #region Utils

        private static RemoteInvokeOptions GetInvokeOptions(bool enabled = false, bool avxEnabled = false)
        {
            var processStartInfo = new ProcessStartInfo();

            processStartInfo.Environment[NumericsHelpers.DisabledEnvironmentVariable] = (!enabled).ToString();
            processStartInfo.Environment[NumericsHelpers.AvxEnvironmentVariable] = avxEnabled.ToString();


            return new RemoteInvokeOptions() {StartInfo = processStartInfo};
        }

        [Flags]
        enum Intrinsics : byte
        {
            None = 0,
            Sse  = 1 << 1,
            Sse2 = 1 << 2,
            Sse3 = 1 << 3,
            Avx  = 1 << 4,
            Avx2 = 1 << 5,

            AdvSimd      = 1 << 6,
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

        [Test]
        public void EnvironmentVariablesWork()
        {
            if (!ValidComputer())
                Assert.Ignore();

            // Disabling both.
            RemoteExecutor.Invoke(() =>
            {
                Assert.That(NumericsHelpers.Enabled, Is.False);
                Assert.That(NumericsHelpers.AvxEnabled, Is.False);
            }, GetInvokeOptions()).Dispose();

            // Enabling NumericsHelper, but not enabling AVX.
            RemoteExecutor.Invoke(() =>
            {
                Assert.That(NumericsHelpers.Enabled, Is.True);
                Assert.That(NumericsHelpers.AvxEnabled, Is.False);
            }, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void EnvironmentVariablesWorkAvx()
        {
            // The next one is only valid if the computer supports AVX.
            if (!ValidComputer(Intrinsics.Avx))
                Assert.Ignore();

            // Enabling NumericsHelper and enabling AVX.
            RemoteExecutor.Invoke(() =>
            {
                Assert.That(NumericsHelpers.Enabled, Is.True);
                Assert.That(NumericsHelpers.AvxEnabled, Is.True);
            }, GetInvokeOptions(true, true)).Dispose();

            // Disabling NumericsHelper and enabling AVX.
            RemoteExecutor.Invoke(() =>
            {
                Assert.That(NumericsHelpers.Enabled, Is.False);
                Assert.That(NumericsHelpers.AvxEnabled, Is.False);
            }, GetInvokeOptions(false, true)).Dispose();
        }

        #region Multiply

        private void Multiply()
        {
            float[] a = new[]
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

            float[] b = new[]
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

            float[] r = new[]
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

            Span<float> s = stackalloc float[r.Length];

            NumericsHelpers.Multiply(a, b, s);

            EqualsApprox(r, s);
        }

        [Test]
        public void MultiplyNaive()
        {
            if (!ValidComputer())
                Assert.Ignore();

            RemoteExecutor.Invoke(Multiply, GetInvokeOptions()).Dispose();
        }

        [Test]
        public void MultiplySse()
        {
            if (!ValidComputer(Intrinsics.Sse))
                Assert.Ignore();

            RemoteExecutor.Invoke(Multiply, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void MultiplyAdvSimd()
        {
            if (!ValidComputer(Intrinsics.AdvSimd))
                Assert.Ignore();

            RemoteExecutor.Invoke(Multiply, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void MultiplyAvx()
        {
            if (!ValidComputer(Intrinsics.Avx))
                Assert.Ignore();

            RemoteExecutor.Invoke(Multiply, GetInvokeOptions(true, true)).Dispose();
        }

        #endregion

        #region MultiplyByScalar

        private void MultiplyByScalar()
        {
            float[] a = new[]
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

            const float b = 50f;

            float[] r = new[]
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

            Span<float> s = stackalloc float[r.Length];

            NumericsHelpers.Multiply(a, b, s);

            EqualsApprox(r, s);
        }

        [Test]
        public void MultiplyByScalarNaive()
        {
            if (!ValidComputer())
                Assert.Ignore();

            RemoteExecutor.Invoke(MultiplyByScalar, GetInvokeOptions()).Dispose();
        }

        [Test]
        public void MultiplyByScalarSse()
        {
            if (!ValidComputer(Intrinsics.Sse))
                Assert.Ignore();

            RemoteExecutor.Invoke(MultiplyByScalar, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void MultiplyByScalarAdvSimd()
        {
            if (!ValidComputer(Intrinsics.AdvSimd))
                Assert.Ignore();

            RemoteExecutor.Invoke(MultiplyByScalar, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void MultiplyByScalarAvx()
        {
            if (!ValidComputer(Intrinsics.Avx))
                Assert.Ignore();

            RemoteExecutor.Invoke(MultiplyByScalar, GetInvokeOptions(true, true)).Dispose();
        }

        #endregion

        #region Divide

        private void Divide()
        {
            float[] a = new[]
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

            float[] b = new[]
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

            float[] r = new[]
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

            Span<float> s = stackalloc float[r.Length];

            NumericsHelpers.Divide(a, b, s);

            EqualsApprox(r, s);
        }

        [Test]
        public void DivideNaive()
        {
            if (!ValidComputer())
                Assert.Ignore();

            RemoteExecutor.Invoke(Divide, GetInvokeOptions()).Dispose();
        }

        [Test]
        public void DivideSse()
        {
            if (!ValidComputer(Intrinsics.Sse))
                Assert.Ignore();

            RemoteExecutor.Invoke(Divide, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void DivideAdvSimd()
        {
            if (!ValidComputer(Intrinsics.AdvSimd))
                Assert.Ignore();

            RemoteExecutor.Invoke(Divide, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void DivideAvx()
        {
            if (!ValidComputer(Intrinsics.Avx))
                Assert.Ignore();

            RemoteExecutor.Invoke(Divide, GetInvokeOptions(true, true)).Dispose();
        }

        #endregion

        #region DivideByScalar

        private void DivideByScalar()
        {
            float[] a = new[]
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

            float b = 1234f;

            float[] r = new[]
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

            Span<float> s = stackalloc float[r.Length];

            NumericsHelpers.Divide(a, b, s);

            EqualsApprox(r, s);
        }

        [Test]
        public void DivideByScalarNaive()
        {
            if (!ValidComputer())
                Assert.Ignore();

            RemoteExecutor.Invoke(DivideByScalar, GetInvokeOptions()).Dispose();
        }

        [Test]
        public void DivideByScalarSse()
        {
            if (!ValidComputer(Intrinsics.Sse))
                Assert.Ignore();

            RemoteExecutor.Invoke(DivideByScalar, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void DivideByScalarAdvSimd()
        {
            if (!ValidComputer(Intrinsics.AdvSimd))
                Assert.Ignore();

            RemoteExecutor.Invoke(DivideByScalar, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void DivideByScalarAvx()
        {
            if (!ValidComputer(Intrinsics.Avx))
                Assert.Ignore();

            RemoteExecutor.Invoke(DivideByScalar, GetInvokeOptions(true, true)).Dispose();
        }

        #endregion

        #region Add

        private void Add()
        {
            float[] a = new[]
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

            float[] b = new[]
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

            float[] r = new[]
            {
                2f,
                1f,
                3f,
                4f,
                110f,
                1.1f,
                5555f,
                1245.357f,
            };

            Span<float> s = stackalloc float[r.Length];

            NumericsHelpers.Add(a, b, s);

            EqualsApprox(r, s);
        }

        [Test]
        public void AddNaive()
        {
            if (!ValidComputer())
                Assert.Ignore();

            RemoteExecutor.Invoke(Add, GetInvokeOptions()).Dispose();
        }

        [Test]
        public void AddSse()
        {
            if (!ValidComputer(Intrinsics.Sse))
                Assert.Ignore();

            RemoteExecutor.Invoke(Add, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void AddAdvSimd()
        {
            if (!ValidComputer(Intrinsics.AdvSimd))
                Assert.Ignore();

            RemoteExecutor.Invoke(Add, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void AddAvx()
        {
            if (!ValidComputer(Intrinsics.Avx))
                Assert.Ignore();

            RemoteExecutor.Invoke(Add, GetInvokeOptions(true, true)).Dispose();
        }

        #endregion

        #region AddByScalar

        private void AddByScalar()
        {
            float[] a = new[]
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

            const float b = 100f;

            float[] r = new[]
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

            Span<float> s = stackalloc float[r.Length];

            NumericsHelpers.Add(a, b, s);

            EqualsApprox(r, s);
        }

        [Test]
        public void AddByScalarNaive()
        {
            if (!ValidComputer())
                Assert.Ignore();

            RemoteExecutor.Invoke(AddByScalar, GetInvokeOptions()).Dispose();
        }

        [Test]
        public void AddByScalarSse()
        {
            if (!ValidComputer(Intrinsics.Sse))
                Assert.Ignore();

            RemoteExecutor.Invoke(AddByScalar, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void AddByScalarAdvSimd()
        {
            if (!ValidComputer(Intrinsics.AdvSimd))
                Assert.Ignore();

            RemoteExecutor.Invoke(AddByScalar, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void AddByScalarAvx()
        {
            if (!ValidComputer(Intrinsics.Avx))
                Assert.Ignore();

            RemoteExecutor.Invoke(AddByScalar, GetInvokeOptions(true, true)).Dispose();
        }

        #endregion

        #region HorizontalAdd

        private void HorizontalAdd()
        {
            float[] a = new[]
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

            const float r = 4042.668f;

            Assert.That(NumericsHelpers.HorizontalAdd(a), Is.Approximately(r));
        }

        [Test]
        public void HorizontalAddNaive()
        {
            if (!ValidComputer())
                Assert.Ignore();

            RemoteExecutor.Invoke(HorizontalAdd, GetInvokeOptions()).Dispose();
        }

        [Test]
        public void HorizontalAddSse()
        {
            if (!ValidComputer(Intrinsics.Sse))
                Assert.Ignore();

            RemoteExecutor.Invoke(HorizontalAdd, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void HorizontalAddAdvSimd()
        {
            if (!ValidComputer(Intrinsics.AdvSimd))
                Assert.Ignore();

            RemoteExecutor.Invoke(HorizontalAdd, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void HorizontalAddAvx()
        {
            if (!ValidComputer(Intrinsics.Avx))
                Assert.Ignore();

            RemoteExecutor.Invoke(HorizontalAdd, GetInvokeOptions(true, true)).Dispose();
        }

        #endregion

        #region Sub

        private void Sub()
        {
            float[] a = new[]
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

            float[] b = new[]
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

            float[] r = new[]
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

            Span<float> s = stackalloc float[r.Length];

            NumericsHelpers.Sub(a, b, s);

            EqualsApprox(r, s);
        }

        [Test]
        public void SubNaive()
        {
            if (!ValidComputer())
                Assert.Ignore();

            RemoteExecutor.Invoke(Sub, GetInvokeOptions()).Dispose();
        }

        [Test]
        public void SubSse()
        {
            if (!ValidComputer(Intrinsics.Sse))
                Assert.Ignore();

            RemoteExecutor.Invoke(Sub, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void SubAdvSimd()
        {
            if (!ValidComputer(Intrinsics.AdvSimd))
                Assert.Ignore();

            RemoteExecutor.Invoke(Sub, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void SubAvx()
        {
            if (!ValidComputer(Intrinsics.Avx))
                Assert.Ignore();

            RemoteExecutor.Invoke(Sub, GetInvokeOptions(true, true)).Dispose();
        }

        #endregion

        #region SubByScalar

        private void SubByScalar()
        {
            float[] a = new[]
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

            const float b = 100f;

            float[] r = new[]
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

            Span<float> s = stackalloc float[r.Length];

            NumericsHelpers.Sub(a, b, s);

            EqualsApprox(r, s);
        }

        [Test]
        public void SubByScalarNaive()
        {
            if (!ValidComputer())
                Assert.Ignore();

            RemoteExecutor.Invoke(SubByScalar, GetInvokeOptions()).Dispose();
        }

        [Test]
        public void SubByScalarSse()
        {
            if (!ValidComputer(Intrinsics.Sse))
                Assert.Ignore();

            RemoteExecutor.Invoke(SubByScalar, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void SubByScalarAdvSimd()
        {
            if (!ValidComputer(Intrinsics.AdvSimd))
                Assert.Ignore();

            RemoteExecutor.Invoke(SubByScalar, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void SubByScalarAvx()
        {
            if (!ValidComputer(Intrinsics.Avx))
                Assert.Ignore();

            RemoteExecutor.Invoke(SubByScalar, GetInvokeOptions(true, true)).Dispose();
        }

        #endregion

        #region Abs

        private void Abs()
        {
            float[] a = new[]
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

            float[] r = new[]
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

            Span<float> s = stackalloc float[r.Length];

            NumericsHelpers.Abs(a, s);

            EqualsApprox(r, s);
        }

        [Test]
        public void AbsNaive()
        {
            if (!ValidComputer())
                Assert.Ignore();

            RemoteExecutor.Invoke(Abs, GetInvokeOptions()).Dispose();
        }

        [Test]
        public void AbsSse()
        {
            if (!ValidComputer(Intrinsics.Sse))
                Assert.Ignore();

            RemoteExecutor.Invoke(Abs, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void AbsAdvSimd()
        {
            if (!ValidComputer(Intrinsics.AdvSimd))
                Assert.Ignore();

            RemoteExecutor.Invoke(Abs, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void AbsAvx()
        {
            if (!ValidComputer(Intrinsics.Avx))
                Assert.Ignore();

            RemoteExecutor.Invoke(Abs, GetInvokeOptions(true, true)).Dispose();
        }

        #endregion

        #region Min

        private void Min()
        {
            float[] a = new[]
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

            float[] b = new[]
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

            float[] r = new[]
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

            Span<float> s = stackalloc float[r.Length];

            NumericsHelpers.Min(a, b, s);

            EqualsApprox(r, s);
        }

        [Test]
        public void MinNaive()
        {
            if (!ValidComputer())
                Assert.Ignore();

            RemoteExecutor.Invoke(Min, GetInvokeOptions()).Dispose();
        }

        [Test]
        public void MinSse()
        {
            if (!ValidComputer(Intrinsics.Sse))
                Assert.Ignore();

            RemoteExecutor.Invoke(Min, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void MinAdvSimd()
        {
            if (!ValidComputer(Intrinsics.AdvSimd))
                Assert.Ignore();

            RemoteExecutor.Invoke(Min, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void MinAvx()
        {
            if (!ValidComputer(Intrinsics.Avx))
                return;

            RemoteExecutor.Invoke(Min, GetInvokeOptions(true, true)).Dispose();
        }

        #endregion

        #region MinByScalar

        private void MinByScalar()
        {
            float[] a = new[]
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

            float b = 1f;

            float[] r = new[]
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

            Span<float> s = stackalloc float[r.Length];

            NumericsHelpers.Min(a, b, s);

            EqualsApprox(r, s);
        }

        [Test]
        public void MinByScalarNaive()
        {
            if (!ValidComputer())
                Assert.Ignore();

            RemoteExecutor.Invoke(MinByScalar, GetInvokeOptions()).Dispose();
        }

        [Test]
        public void MinByScalarSse()
        {
            if (!ValidComputer(Intrinsics.Sse))
                Assert.Ignore();

            RemoteExecutor.Invoke(MinByScalar, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void MinByScalarAdvSimd()
        {
            if (!ValidComputer(Intrinsics.AdvSimd))
                Assert.Ignore();

            RemoteExecutor.Invoke(MinByScalar, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void MinByScalarAvx()
        {
            if (!ValidComputer(Intrinsics.Avx))
                Assert.Ignore();

            RemoteExecutor.Invoke(MinByScalar, GetInvokeOptions(true, true)).Dispose();
        }

        #endregion

        #region Max

        private void Max()
        {
            float[] a = new[]
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

            float[] b = new[]
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

            float[] r = new[]
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

            Span<float> s = stackalloc float[r.Length];

            NumericsHelpers.Max(a, b, s);

            EqualsApprox(r, s);
        }

        [Test]
        public void MaxNaive()
        {
            if (!ValidComputer())
                Assert.Ignore();

            RemoteExecutor.Invoke(Max, GetInvokeOptions()).Dispose();
        }

        [Test]
        public void MaxSse()
        {
            if (!ValidComputer(Intrinsics.Sse))
                Assert.Ignore();

            RemoteExecutor.Invoke(Max, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void MaxAdvSimd()
        {
            if (!ValidComputer(Intrinsics.AdvSimd))
                Assert.Ignore();

            RemoteExecutor.Invoke(Max, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void MaxAvx()
        {
            if (!ValidComputer(Intrinsics.Avx))
                Assert.Ignore();

            RemoteExecutor.Invoke(Max, GetInvokeOptions(true, true)).Dispose();
        }

        #endregion

        #region MaxByScalar

        private void MaxByScalar()
        {
            float[] a = new[]
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

            float b = 100f;

            float[] r = new[]
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

            Span<float> s = stackalloc float[r.Length];

            NumericsHelpers.Max(a, b, s);

            EqualsApprox(r, s);
        }

        [Test]
        public void MaxByScalarNaive()
        {
            if (!ValidComputer())
                Assert.Ignore();

            RemoteExecutor.Invoke(MaxByScalar, GetInvokeOptions()).Dispose();
        }

        [Test]
        public void MaxByScalarSse()
        {
            if (!ValidComputer(Intrinsics.Sse))
                Assert.Ignore();

            RemoteExecutor.Invoke(MaxByScalar, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void MaxByScalarAdvSimd()
        {
            if (!ValidComputer(Intrinsics.AdvSimd))
                Assert.Ignore();

            RemoteExecutor.Invoke(MaxByScalar, GetInvokeOptions(true)).Dispose();
        }

        [Test]
        public void MaxByScalarAvx()
        {
            if (!ValidComputer(Intrinsics.Avx))
                Assert.Ignore();

            RemoteExecutor.Invoke(MaxByScalar, GetInvokeOptions(true, true)).Dispose();
        }

        #endregion
    }
}
