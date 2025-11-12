namespace Robust.Shared.Physics.Systems;

public abstract partial class SharedPhysicsSystem
{
    protected void DispatchEvents()
    {
        // This will raise events even if the contact is gone which is fine I think
        // because otherwise we may get issues with events not getting raised in some cases.

        // Swap the end index over so new events happen next tick.
        _endEventIndex = 1 - _endEventIndex;

        // Raises all the buffered events once physics step is done.
        foreach (var ev in _startCollideEvents)
        {
            var elem = ev;
            RaiseLocalEvent(ev.OurEntity, ref elem);
        }

        _startCollideEvents.Clear();

        foreach (var ev in _endCollideEvents[1 - _endEventIndex])
        {
            var elem = ev;
            RaiseLocalEvent(ev.OurEntity, ref elem);
        }
        
        _endCollideEvents[1 - _endEventIndex].Clear();
    }
}
