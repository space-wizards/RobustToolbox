using NUnit.Framework;
using Robust.Client.Audio;
using Robust.Shared;
using Robust.Shared.IoC;
using Robust.UnitTesting;

namespace Robust.Client.IntegrationTests.Audio;

[TestFixture]
[TestOf(typeof(AudioManager))]
[Explicit("Server test-runners don't typically have the means of running OpenAL.")]
public sealed class AudioManagerTest : RobustIntegrationTest
{
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
            Assert.That(client.CfgMan.GetCVar(CVars.AudioDevice), Is.EqualTo(testDevice));

            client.CfgMan.SetCVar(CVars.AudioDevice, string.Empty);
            Assert.That(client.CfgMan.GetCVar(CVars.AudioDevice), Is.Empty);
        });
    }
}
