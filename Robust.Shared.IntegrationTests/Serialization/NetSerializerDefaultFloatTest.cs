using JetBrains.Annotations;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.UnitTesting.Shared;

namespace Robust.Shared.IntegrationTests.Serialization;

[Serializable, NetSerializable]
[UsedImplicitly(Reason = "Needed so RobustSerializer is guaranteed to pick up on the unsafe types.")]
internal sealed class MakeTheseSerializable
{
    public UnsafeFloat Single;
    public UnsafeDouble Double;
    public UnsafeHalf Half;
    public Half SafeHalf;
}

/// <summary>
/// Tests the serialization behavior of float types when <see cref="IRobustSerializer"/> is *not* set to do anything special.
/// Tests both primitives and Robust's "Unsafe" variants.
/// </summary>
[TestFixture, TestOf(typeof(RobustSerializer)), TestOf(typeof(NetUnsafeFloatSerializer))]
internal sealed class NetSerializerDefaultFloatTest : OurRobustUnitTest
{
    private IRobustSerializer _serializer = null!;

    [OneTimeSetUp]
    public void Setup()
    {
        _serializer = IoCManager.Resolve<IRobustSerializer>();
        _serializer.Initialize();
    }

    internal static readonly TestCaseData[] PassThroughFloatTests =
    [
        new TestCaseData(0.0).Returns(0.0),
        new TestCaseData(1.0).Returns(1.0),
        new TestCaseData(double.NaN).Returns(double.NaN),
        new TestCaseData(double.PositiveInfinity).Returns(double.PositiveInfinity),
    ];

    [TestCaseSource(nameof(PassThroughFloatTests))]
    public double TestSingle(double input)
    {
        var ms = new MemoryStream();
        _serializer.Serialize(ms, (float)input);

        ms.Position = 0;

        return _serializer.Deserialize<float>(ms);
    }

    [TestCaseSource(nameof(PassThroughFloatTests))]
    public double TestUnsafeSingle(double input)
    {
        var ms = new MemoryStream();
        _serializer.Serialize(ms, (UnsafeFloat)input);

        ms.Position = 0;

        return _serializer.Deserialize<UnsafeFloat>(ms);
    }

    [TestCaseSource(nameof(PassThroughFloatTests))]
    public double TestDouble(double input)
    {
        var ms = new MemoryStream();
        _serializer.Serialize(ms, input);

        ms.Position = 0;

        return _serializer.Deserialize<double>(ms);
    }

    [TestCaseSource(nameof(PassThroughFloatTests))]
    public double TestUnsafeDouble(double input)
    {
        var ms = new MemoryStream();
        _serializer.Serialize(ms, (UnsafeDouble)input);

        ms.Position = 0;

        return _serializer.Deserialize<UnsafeDouble>(ms);
    }

    [TestCaseSource(nameof(PassThroughFloatTests))]
    public double TestHalf(double input)
    {
        var ms = new MemoryStream();
        _serializer.Serialize(ms, (Half)input);

        ms.Position = 0;

        return (double)_serializer.Deserialize<Half>(ms);
    }

    [TestCaseSource(nameof(PassThroughFloatTests))]
    public double TestUnsafeHalf(double input)
    {
        var ms = new MemoryStream();
        _serializer.Serialize(ms, (UnsafeHalf)(Half)input);

        ms.Position = 0;

        return (double)(Half)_serializer.Deserialize<UnsafeHalf>(ms);
    }
}

/// <summary>
/// Tests the serialization behavior of float types when <see cref="IRobustSerializer"/> is set to remove NaNs on read.
/// Tests both primitives and Robust's "Unsafe" variants.
/// </summary>
[TestFixture]
[TestOf(typeof(RobustSerializer)), TestOf(typeof(NetUnsafeFloatSerializer)), TestOf(typeof(NetSafeFloatSerializer))]
internal sealed class NetSerializerSafeFloatTest : OurRobustUnitTest
{
    private IRobustSerializer _serializer = default!;

    [OneTimeSetUp]
    public void Setup()
    {
        _serializer = IoCManager.Resolve<IRobustSerializer>();
        _serializer.FloatFlags = SerializerFloatFlags.RemoveReadNan;
        _serializer.Initialize();
    }

    internal static readonly TestCaseData[] SafeFloatTests =
    [
        new TestCaseData(0.0).Returns(0.0),
        new TestCaseData(1.0).Returns(1.0),
        new TestCaseData(double.NaN).Returns(0.0),
        new TestCaseData(double.PositiveInfinity).Returns(double.PositiveInfinity),
    ];

    [TestCaseSource(nameof(SafeFloatTests))]
    public double TestSingle(double input)
    {
        var ms = new MemoryStream();
        _serializer.Serialize(ms, (float)input);

        ms.Position = 0;

        return _serializer.Deserialize<float>(ms);
    }

    [TestCaseSource(typeof(NetSerializerDefaultFloatTest), nameof(NetSerializerDefaultFloatTest.PassThroughFloatTests))]
    public double TestUnsafeSingle(double input)
    {
        var ms = new MemoryStream();
        _serializer.Serialize(ms, (UnsafeFloat)input);

        ms.Position = 0;

        return _serializer.Deserialize<UnsafeFloat>(ms);
    }

    [TestCaseSource(nameof(SafeFloatTests))]
    public double TestDouble(double input)
    {
        var ms = new MemoryStream();
        _serializer.Serialize(ms, input);

        ms.Position = 0;

        return _serializer.Deserialize<double>(ms);
    }

    [TestCaseSource(typeof(NetSerializerDefaultFloatTest), nameof(NetSerializerDefaultFloatTest.PassThroughFloatTests))]
    public double TestUnsafeDouble(double input)
    {
        var ms = new MemoryStream();
        _serializer.Serialize(ms, (UnsafeDouble)input);

        ms.Position = 0;

        return _serializer.Deserialize<UnsafeDouble>(ms);
    }


    [TestCaseSource(nameof(SafeFloatTests))]
    public double TestHalf(double input)
    {
        var ms = new MemoryStream();
        _serializer.Serialize(ms, (Half)input);

        ms.Position = 0;

        return (double)_serializer.Deserialize<Half>(ms);
    }

    [TestCaseSource(typeof(NetSerializerDefaultFloatTest), nameof(NetSerializerDefaultFloatTest.PassThroughFloatTests))]
    public double TestUnsafeHalf(double input)
    {
        var ms = new MemoryStream();
        _serializer.Serialize(ms, (UnsafeHalf)(Half)input);

        ms.Position = 0;

        return (double)(Half)_serializer.Deserialize<UnsafeHalf>(ms);
    }
}
