using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Shared.Timing;
public sealed class TimedEventSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    private readonly ComponentTimedEvent _componentTimedEventInstance = new();
    private readonly PriorityQueue<(Component Component, string Key), TimeSpan> _timeQueue = new ();

    public override void Update(float frameTime)
    {
        if (_gameTiming.Paused) return;
        var time = _gameTiming.CurTime;
        while (_timeQueue.TryDequeue(out var pair, out var triggerTime))
        {
            if (triggerTime <= time)
            {
                _componentTimedEventInstance.Key = pair.Key;
                EntityManager.EventBus.RaiseComponentEvent(pair.Component, _componentTimedEventInstance);
            }
            else
            {
                _timeQueue.Enqueue(pair, triggerTime);
                break;
            }
        }
    }

    [PublicAPI]
    public void AddTimedEvent<TComponent>(TComponent component, TimeSpan waitTime, string key) where TComponent: Component
    {
        _timeQueue.Enqueue((component, key), _gameTiming.RealTime+waitTime);
    }

}

/// <summary>
/// Raised when a timed event happens on a component
/// </summary>
[ComponentEvent]
public sealed class ComponentTimedEvent : EntityEventArgs
{
    public string? Key { get; set; }
}
