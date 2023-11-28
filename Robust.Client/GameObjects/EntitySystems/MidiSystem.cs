using Robust.Client.Audio;
using Robust.Client.Audio.Midi;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.GameObjects
{
    public sealed class MidiSystem : EntitySystem
    {
        [Dependency] private readonly IMidiManager _midiManager = default!;

        public override void Initialize()
        {
            base.Initialize();
            // AudioSystem sets eye position and rotation so rely on those.
            UpdatesAfter.Add(typeof(AudioSystem));
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);
            _midiManager.FrameUpdate(frameTime);
        }
    }
}
