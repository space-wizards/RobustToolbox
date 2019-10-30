using Robust.Client.Audio.Midi;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.IoC;

namespace Robust.Client.GameObjects.EntitySystems
{
    public class MidiSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IMidiManager _midiManager;
#pragma warning restore 649

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);
            _midiManager.FrameUpdate(frameTime);
        }
    }
}
