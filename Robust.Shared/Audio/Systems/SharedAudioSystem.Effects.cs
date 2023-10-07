using Robust.Shared.Audio.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Robust.Shared.Audio.Systems;

public abstract partial class SharedAudioSystem
{
    public void SetAuxiliary(EntityUid uid, AudioComponent audio, EntityUid auxiliaryUid, AudioAuxiliaryComponent auxiliary)
    {
        DebugTools.AssertOwner(auxiliaryUid, auxiliary);
        audio.Auxiliary = auxiliaryUid;
        Dirty(uid, audio);
    }
}
