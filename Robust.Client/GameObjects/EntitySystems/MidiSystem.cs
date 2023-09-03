using Robust.Client.Audio.Midi;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.GameObjects
{
    public sealed partial class MidiSystem : EntitySystem
    {
        [Dependency] private IMidiManager _midiManager = default!;

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);
            _midiManager.FrameUpdate(frameTime);
        }
    }
}
