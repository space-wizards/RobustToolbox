using NUnit.Framework;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Audio;

[TestFixture]
public sealed class AudioGainVolume_Test
{
    private static float[] _gainValues = new[]
    {
        1f,
        0.5f,
        0f,
    };

    private static float[] _volumeValues = new[]
    {
        -100f,
        -3f,
        0f,
        1f,
        100f,
    };

    [Test, TestCaseSource(nameof(_gainValues))]
    public void GainCalculationTest(float value)
    {
        var volume = SharedAudioSystem.GainToVolume(value);
        var gain = SharedAudioSystem.VolumeToGain(volume);

        Assert.That(MathHelper.CloseTo(value, gain, 0.01f), $"Expected {value} and found {volume}");
    }

    [Test, TestCaseSource(nameof(_volumeValues))]
    public void VolumeCalculationTest(float value)
    {
        var gain = SharedAudioSystem.VolumeToGain(value);
        var volume = SharedAudioSystem.GainToVolume(gain);

        Assert.That(MathHelper.CloseTo(value, volume, 0.01f), $"Expected {value} and found {volume}");
    }
}
