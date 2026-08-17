using NUnit.Framework;
using Robust.Client.Audio;
using Robust.Shared;
using Robust.Shared.Audio;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.UnitTesting;
using System.Numerics;

namespace Robust.Client.IntegrationTests.Audio;

[TestFixture]
[TestOf(typeof(AudioManager))]
[Explicit("Server test-runners don't typically have the means of running OpenAL.")]
public sealed class AudioManagerTest : RobustIntegrationTest
{
    [Test]
    public async Task SurvivesWithoutOutputDevice()
    {
        // ALSOFT_DRIVERS=null forces the silent backend even on a machine with real output.
        Environment.SetEnvironmentVariable("ALSOFT_DRIVERS", "null");

        var client = StartClient(new ClientIntegrationOptions
        {
            Pool = false,
            InitIoC = () =>
            {
                IoCManager.Register<IAudioManager, AudioManager>(overwrite: true);
                IoCManager.Register<IAudioInternal, AudioManager>(overwrite: true);
            },
        });
        await client.WaitIdleAsync();

        var audio = client.ResolveDependency<IAudioInternal>();

        await client.WaitAssertion(() =>
        {
            Assert.That(audio.IsInitialized, Is.True, "null backend should still initialise");

            // None of these may throw with no real output.
            Assert.DoesNotThrow(() => audio.SetPosition(Vector2.Zero));
            Assert.DoesNotThrow(() => audio.SetRotation(Angle.Zero));
            Assert.DoesNotThrow(() => audio.StopAllAudio());

            var stream = audio.LoadAudioRaw(new short[1024], 1, 44100, "test");
            Assert.That(stream, Is.Not.Null);

            var source = audio.CreateAudioSource(stream);
            Assert.DoesNotThrow(() => source?.StartPlaying());
            source?.Dispose();
        });
    }

    [Test]
    public async Task SwitchesAudioDevice()
    {
        var client = StartClient(new ClientIntegrationOptions
        {
            Pool = false,
            InitIoC = () =>
            {
                IoCManager.Register<IAudioManager, AudioManager>(overwrite: true);
                IoCManager.Register<IAudioInternal, AudioManager>(overwrite: true);
            },
        });
        await client.WaitIdleAsync();

        var audio = client.ResolveDependency<IAudioManager>();
        Assert.That(audio, Is.TypeOf<AudioManager>());

        var defaultDevice = audio.GetDefaultAudioDevice();
        var devices = audio.GetAudioDevices();
        var testDevice = devices.FirstOrDefault(device => device != defaultDevice) ?? defaultDevice;

        if (testDevice == null)
            Assert.Ignore("OpenAL did not expose any audio output devices.");

        await client.WaitAssertion(() =>
        {
            client.CfgMan.SetCVar(CVars.AudioDevice, testDevice);
            Assert.That(audio.GetCurrentDeviceName(), Does.Contain(testDevice).IgnoreCase);

            client.CfgMan.SetCVar(CVars.AudioDevice, string.Empty);
            Assert.That(audio.IsInitialized, Is.True);
        });
    }
}

[TestFixture]
[TestOf(typeof(AudioManager))]
public sealed class AudioAttenuationTest
{
    [Test]
    public void GainIsOneAtReferenceDistance(
        [Values(Attenuation.InverseDistance, Attenuation.InverseDistanceClamped,
                Attenuation.LinearDistance, Attenuation.LinearDistanceClamped,
                Attenuation.ExponentDistance, Attenuation.ExponentDistanceClamped)]
        Attenuation attenuation)
    {
        var gain = AudioManager.GetAttenuationGain(attenuation, 5f, 1f, 5f, 50f);
        Assert.That(gain, Is.EqualTo(1f).Within(0.0001f));
    }

    [Test]
    public void GainNeverLeavesUnitRange(
        [Values(Attenuation.NoAttenuation,
        Attenuation.InverseDistance, Attenuation.InverseDistanceClamped,
        Attenuation.LinearDistance, Attenuation.LinearDistanceClamped,
        Attenuation.ExponentDistance, Attenuation.ExponentDistanceClamped)] Attenuation attenuation,
        [Values(0f, 0.1f, 5f, 49f, 50f, 1000f, float.MaxValue)] float distance)
    {
        var gain = AudioManager.GetAttenuationGain(attenuation, distance, 1f, 5f, 50f);

        Assert.That(gain, Is.Not.NaN);
        Assert.That(gain, Is.InRange(0f, 1f));
    }

    [Test]
    public void GainIsMonotonicallyDecreasing([Values(Attenuation.NoAttenuation,
        Attenuation.InverseDistance, Attenuation.InverseDistanceClamped,
        Attenuation.LinearDistance, Attenuation.LinearDistanceClamped,
        Attenuation.ExponentDistance, Attenuation.ExponentDistanceClamped)] Attenuation attenuation)
    {
        var previous = float.MaxValue;

        for (var d = 0f; d <= 100f; d += 0.5f)
        {
            var gain = AudioManager.GetAttenuationGain(attenuation, d, 1f, 5f, 50f);
            Assert.That(gain, Is.LessThanOrEqualTo(previous).Within(0.0001f));
            previous = gain;
        }
    }

    [Test]
    public void DegenerateRangeDoesNotProduceNaN()
    {
        // referenceDistance > maxDistance is reachable through config.
        foreach (Attenuation a in Enum.GetValues<Attenuation>())
        {
            try
            {
                var gain = AudioManager.GetAttenuationGain(a, 10f, 1f, 50f, 5f);

                Assert.That(gain, Is.Not.NaN, $"{a} produced NaN");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Assert.That(a == Attenuation.Invalid, $"Got \"No attenuation formula for {a}!\" when {a} is not Invalid!!!");
            }
        }
    }

    [TestCase("No Output", ExpectedResult = true)]
    [TestCase("OpenAL Soft on No Output", ExpectedResult = true)]
    [TestCase("OpenAL Soft on Headphones (Realtek)", ExpectedResult = false)]
    [TestCase("", ExpectedResult = false)]
    public bool RecognisesNullDevice(string name) => AudioManager.IsNullDevice(name);
}
