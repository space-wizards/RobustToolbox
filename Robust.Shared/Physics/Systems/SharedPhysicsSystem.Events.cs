namespace Robust.Shared.Physics.Systems;

public abstract partial class SharedPhysicsSystem
{
    protected void DispatchEvents()
    {
        // This will raise events even if the contact is gone which is fine I think
        // because otherwise we may get issues with events not getting raised in some cases.

        // Raises all the buffered events once physics step is done.
        foreach (var ev in _startCollideEvents)
        {
            var elem = ev;
            RaiseLocalEvent(ev.OurEntity, ref elem);
        }

        foreach (var ev in _endCollideEvents[_endEventIndex])
        {
            var elem = ev;
            RaiseLocalEvent(ev.OurEntity, ref elem);
        }

        _endEventIndex = 1 - _endEventIndex;
        _startCollideEvents.Clear();
        _endCollideEvents[_endEventIndex].Clear();
    }
}
