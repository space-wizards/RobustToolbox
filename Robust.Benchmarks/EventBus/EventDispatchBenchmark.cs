using System;
using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.UnitTesting.Server;

namespace Robust.Benchmarks.EventBus;

[Virtual]
public class EventDispatchBenchmark
{
    private ISimulation _serverSim = default!;
    private IEntityManager _entMan = default!;
    private IEventBus _eventbus = default!;
    private EntityUid _entity = default;

    private int _accumulator = 0;

    public delegate void ClassEventDelegate(ClassEvent ev);
    public delegate void StructEventDelegate(StructEvent ev);
    public delegate void ByRefStructEventDelegate(ref ByRefStructEvent ev);

    public delegate void DirectedClassEventDelegate(EntityUid uid, ClassEvent ev);
    public delegate void DirectedStructEventDelegate(EntityUid uid, StructEvent ev);
    public delegate void DirectedByRefStructEventDelegate(EntityUid uid, ref ByRefStructEvent ev);

    public event ClassEventDelegate? ActionClassEvent;
    public event StructEventDelegate? ActionStructEvent;
    public event ByRefStructEventDelegate? ActionByRefStructEvent;

    public event DirectedClassEventDelegate? DirectedActionClassEvent;
    public event DirectedStructEventDelegate? DirectedActionStructEvent;
    public event DirectedByRefStructEventDelegate? DirectedActionByRefStructEvent;

    [Params(1, 10, 100, 1000)]
    public int N { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _serverSim = RobustServerSimulation.NewSimulation()
            .RegisterComponents(factory => factory.RegisterClass<EventDispatchTestComponent>())
            .RegisterEntitySystems(factory => factory.LoadExtraSystemType<EventDispatchTestSystem>())
            .InitializeInstance();

        _entMan = _serverSim.Resolve<IEntityManager>();
        _eventbus = _entMan.EventBus;
        _entity = _entMan.SpawnEntity(null, MapCoordinates.Nullspace);
        _entMan.AddComponent<EventDispatchTestComponent>(_entity);

        ActionClassEvent += ClassEventHandler;
        ActionStructEvent += StructEventHandler;
        ActionByRefStructEvent += ByRefStructEventHandler;

        DirectedActionClassEvent += DirectedClassEventHandler;
        DirectedActionStructEvent += DirectedStructEventHandler;
        DirectedActionByRefStructEvent += DirectedByRefStructEventHandler;
    }

    [Benchmark]
    public void DirectedClassEvent()
    {
        for (var i = 0; i < N; i++)
        {
            var ev = new ClassEvent(i);
            _eventbus.RaiseLocalEvent(_entity, ev);
        }
    }

    [Benchmark]
    public void DirectedStructEvent()
    {
        for (var i = 0; i < N; i++)
        {
            var ev = new StructEvent(i);
            _eventbus.RaiseLocalEvent(_entity, ev);
        }
    }

    [Benchmark]
    public void DirectedByRefStructEvent()
    {
        for (var i = 0; i < N; i++)
        {
            var ev = new ByRefStructEvent(i);
            _eventbus.RaiseLocalEvent(_entity, ref ev);
        }
    }

    [Benchmark]
    public void BroadcastClassEvent()
    {
        for (var i = 0; i < N; i++)
        {
            var ev = new ClassEvent(i);
            _eventbus.RaiseEvent(EventSource.Local, ev);
        }
    }

    [Benchmark]
    public void BroadcastStructEvent()
    {
        for (var i = 0; i < N; i++)
        {
            var ev = new StructEvent(i);
            _eventbus.RaiseEvent(EventSource.Local, ev);
        }
    }

    [Benchmark]
    public void BroadcastByRefStructEvent()
    {
        for (var i = 0; i < N; i++)
        {
            var ev = new ByRefStructEvent(i);
            _eventbus.RaiseEvent(EventSource.Local, ref ev);
        }
    }

    [Benchmark]
    public void CSharpClassEvent()
    {
        for (var i = 0; i < N; i++)
        {
            var ev = new ClassEvent(i);
            ActionClassEvent?.Invoke(ev);
        }
    }

    [Benchmark]
    public void CSharpStructEvent()
    {
        for (var i = 0; i < N; i++)
        {
            var ev = new StructEvent(i);
            ActionStructEvent?.Invoke(ev);
        }
    }

    [Benchmark]
    public void CSharpByRefEvent()
    {
        for (var i = 0; i < N; i++)
        {
            var ev = new ByRefStructEvent(i);
            ActionByRefStructEvent?.Invoke(ref ev);
        }
    }

    [Benchmark]
    public void CSharpDirectedClassEvent()
    {
        for (var i = 0; i < N; i++)
        {
            var ev = new ClassEvent(i);
            DirectedActionClassEvent?.Invoke(_entity, ev);
        }
    }

    [Benchmark]
    public void CSharpDirectedStructEvent()
    {
        for (var i = 0; i < N; i++)
        {
            var ev = new StructEvent(i);
            DirectedActionStructEvent?.Invoke(_entity, ev);
        }
    }

    [Benchmark]
    public void CSharpDirectedByRefEvent()
    {
        for (var i = 0; i < N; i++)
        {
            var ev = new ByRefStructEvent(i);
            DirectedActionByRefStructEvent?.Invoke(_entity, ref ev);
        }
    }

    private void ClassEventHandler(ClassEvent ev)
    {
        _accumulator += ev.Value;
    }

    private void StructEventHandler(StructEvent ev)
    {
        _accumulator += ev.Value;
    }

    private void ByRefStructEventHandler(ref ByRefStructEvent ev)
    {
        _accumulator += ev.Value;
    }

    private void DirectedClassEventHandler(EntityUid uid, ClassEvent ev)
    {
        if(_entMan.TryGetComponent(uid, out EventDispatchTestComponent? comp))
            comp.Accumulator += ev.Value;
    }

    private void DirectedStructEventHandler(EntityUid uid, StructEvent ev)
    {
        if(_entMan.TryGetComponent(uid, out EventDispatchTestComponent? comp))
            comp.Accumulator += ev.Value;
    }

    private void DirectedByRefStructEventHandler(EntityUid uid, ref ByRefStructEvent ev)
    {
        if(_entMan.TryGetComponent(uid, out EventDispatchTestComponent? comp))
            comp.Accumulator += ev.Value;
    }

    public class ClassEvent : EntityEventArgs
    {
        public readonly int Value;

        public ClassEvent(int value)
        {
            Value = value;
        }
    }

    public struct StructEvent
    {
        public readonly int Value;

        public StructEvent(int value)
        {
            Value = value;
        }
    }

    [ByRefEvent]
    public struct ByRefStructEvent
    {
        public readonly int Value;

        public ByRefStructEvent(int value)
        {
            Value = value;
        }
    }

    public class EventDispatchTestComponent : Component
    {
        public int Accumulator;
    }

    public class EventDispatchTestSystem : EntitySystem
    {
        public int Accumulator = 0;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ClassEvent>(HandleBroadcastClassEvent);
            SubscribeLocalEvent<StructEvent>(HandleBroadcastStructEvent);
            SubscribeLocalEvent<ByRefStructEvent>(HandleBroadcastByRefStructEvent);
            SubscribeLocalEvent<EventDispatchTestComponent, ClassEvent>(HandleDirectedClassEvent);
            SubscribeLocalEvent<EventDispatchTestComponent, StructEvent>(HandleDirectedStructEvent);
            SubscribeLocalEvent<EventDispatchTestComponent, ByRefStructEvent>(HandleDirectedByRefStructEvent);
        }

        private void HandleBroadcastClassEvent(ClassEvent ev)
        {
            Accumulator += ev.Value;
        }

        private void HandleBroadcastStructEvent(StructEvent ev)
        {
            Accumulator += ev.Value;
        }

        private void HandleBroadcastByRefStructEvent(ref ByRefStructEvent ev)
        {
            Accumulator += ev.Value;
        }

        private void HandleDirectedClassEvent(EntityUid uid, EventDispatchTestComponent component, ClassEvent ev)
        {
            component.Accumulator += ev.Value;
        }

        private void HandleDirectedStructEvent(EntityUid uid, EventDispatchTestComponent component, StructEvent ev)
        {
            component.Accumulator += ev.Value;
        }

        private void HandleDirectedByRefStructEvent(EntityUid uid, EventDispatchTestComponent component, ref ByRefStructEvent ev)
        {
            component.Accumulator += ev.Value;
        }
    }
}
