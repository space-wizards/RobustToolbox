using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Animus.States;
using Robust.Client.Animus.Triggers;
using Robust.Client.GameObjects;
using Robust.Client.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;

namespace Robust.Client.Animus;

public sealed class AnimusSystem : EntitySystem
{
    private const float UpdateInterval = 0.1f;

    [Dependency] private readonly ILogManager _logger = default!;
    [Dependency] private readonly IClientGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;
    private ISawmill _sawmill = default!;
    private readonly Dictionary<EntityUid, List<(Type, string)>> _actingComponentProperties = new();
    // TODO: Subscribe to OnAnimationStarted event to register running legacy animations.

    private void OnAnimationCompleted(Entity<AnimationPlayerComponent> entity, ref AnimationCompletedEvent args)
    {
        // TODO: Deregister legacy animations.

        if (!TryComp<AnimusComponent>(entity, out var comp))
            return;

        AnimusInstance? animusInstance = null;
        foreach (var machine in comp.ActiveStateMachines)
        {
            if (machine.ActiveState is AnimusStateAnimation animState && animState.RunningAnimationKey == args.Key)
            {
                animusInstance = machine;
            }
        }

        if (animusInstance == null)
            return;

        if (animusInstance.ActiveState.OneShot)
        {
            SwitchState((entity, comp),
                animusInstance.DefaultState,
                false);
        }
        else if (EvaluateConditions((entity, comp), animusInstance.ActiveState))
        {
            animusInstance.ActiveState.Update(entity, args.Finished);
        }
    }

    private void OnAnimationStateMachineComponentInit(Entity<AnimusComponent> entity, ref ComponentInit args)
    {
        foreach (var animusInstance in entity.Comp.StateMachines)
        {
            InitializeStateMachine(entity, animusInstance);
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logger.GetSawmill("asm");

        SubscribeLocalEvent<AnimusComponent, ComponentInit>(OnAnimationStateMachineComponentInit);
        SubscribeLocalEvent<AnimationPlayerComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<AnimusComponent>();
        while (query.MoveNext(out var entity, out var comp))
        {
            foreach (var animusInstance in comp.ActiveStateMachines)
            {
                UpdateStateMachine((entity, comp), animusInstance);
            }
        }
    }

    internal void TriggerInternal(AnimusStateBase state, Entity<AnimusComponent> entity)
    {
        foreach (var cond in state.Conditions)
        {
            if (!cond.EvaluateInternal(entity))
                return;
        }

        SwitchState(entity, state, true);
    }

    internal void RegisterEntityAnimationProperty(EntityUid uid, Type type, string prop)
    {
        if (!_actingComponentProperties.ContainsKey(uid))
            _actingComponentProperties.Add(uid, []);
        if (_actingComponentProperties[uid]
            .Any(x => x.Item1 == type && x.Item2 == prop))
        {
            _sawmill.Error($"An animation using the {prop} property on {type.Name} has already been registered for entity {uid}");
            return;
        }
        _actingComponentProperties[uid].Add((type, prop));
    }

    internal void DeregisterEntityAnimationProperty(EntityUid uid, Type type, string prop)
    {
        if(!_actingComponentProperties.ContainsKey(uid))
            _actingComponentProperties.Add(uid, []);
        var tuple = _actingComponentProperties[uid].Single(x => x.Item1 == type && x.Item2 == prop);
        _actingComponentProperties[uid].Remove(tuple);
    }

    /// <summary>
    /// Initialize state machine from prototype and add to list of active instances.
    /// </summary>
    private void InitializeStateMachine(Entity<AnimusComponent> entity, ProtoId<AnimusPrototype> protoId)
    {
        List<AnimusStateBase> states = [];

        var proto = _prototypeManager.Index(protoId);

        var animusInstance = new AnimusInstance()
        {
            Prototype = proto,
            Timer = _serializationManager.CreateCopy(proto.Timer, null, false),
            DefaultState = _serializationManager.CreateCopy(proto.DefaultState, null, false, false),
            NextUpdate = _timing.CurTime + TimeSpan.FromSeconds(UpdateInterval),
        };

        animusInstance.ActiveState.Initialize(entity, EntityManager, animusInstance);
        animusInstance.DefaultState.Initialize(entity, EntityManager, animusInstance);

        foreach (var state in proto.States)
        {
            // TODO: Add more info to error message.
            if (state.Conditions.Length == 0)
            {
                _sawmill.Error("Every AnimusState must have at least one condition. (Except DefaultState)");
                continue;
            }

            states.Add(CopyInitializeState(entity, state, animusInstance));
        }

        animusInstance.States = states.ToArray();
        animusInstance.DefaultState = CopyInitializeState(entity, proto.DefaultState, animusInstance);

        entity.Comp.ActiveStateMachines.Add(animusInstance);
    }

    private AnimusStateBase CopyInitializeState(Entity<AnimusComponent> entity, AnimusStateBase state, AnimusInstance animusInstance)
    {
        var stateCopy = _serializationManager.CreateCopy(state, null, false, false);
        stateCopy.Initialize(entity.Owner, EntityManager, animusInstance);

        foreach (var cond in stateCopy.Conditions)
        {
            cond.Initialize(EntityManager);
        }

        foreach (var trigger in stateCopy.Triggers)
        {
            trigger.InitializeInternal(EntityManager, entity, stateCopy);
        }

        return stateCopy;
    }

    private void UpdateStateMachine(Entity<AnimusComponent> entity, AnimusInstance animusInstance)
    {
        UpdateTriggers(entity, animusInstance);
        ExitActiveStateIfExitTimerReached(entity, animusInstance);

        if (animusInstance.NextUpdate > _timing.CurTime)
            return;

        CheckStateMachineConditionsAndUpdateState(entity, animusInstance);

        animusInstance.NextUpdate = _timing.CurTime + (
            animusInstance.Timer?.GetNextPeriod(_random) ??
            TimeSpan.FromSeconds(UpdateInterval)
        );
    }

    private void ExitActiveStateIfExitTimerReached(Entity<AnimusComponent> entity, AnimusInstance animusInstance)
    {
        if (animusInstance.ActiveStateExitTime == TimeSpan.Zero)
            return;

        if (animusInstance.ActiveStateExitTime > _timing.CurTime)
            return;

        SwitchState(entity, animusInstance.DefaultState, false);
        animusInstance.ActiveStateExitTime = TimeSpan.Zero;
    }

    private void CheckStateMachineConditionsAndUpdateState(Entity<AnimusComponent> entity, AnimusInstance animusInstance)
    {
        var currentState = animusInstance.ActiveState;
        var nextState = animusInstance.DefaultState;

        // Return if currentState has conditions that are still fulfilled.
        if (currentState is { Conditions.Length: > 0, OneShot: false } &&
            EvaluateConditions(entity, currentState) &&
            animusInstance.ActiveStateExitTime == TimeSpan.Zero)
            return;

        foreach (var state in animusInstance.States)
        {
            if (!EvaluateConditions(entity, state))
                continue;

            nextState = state;
            break;
        }

        SwitchState(entity, nextState, false);
        if (nextState.ExitPeriod != TimeSpan.Zero)
            animusInstance.ActiveStateExitTime = _timing.CurTime + nextState.ExitPeriod;
    }

    private void UpdateTriggers(Entity<AnimusComponent> entity, AnimusInstance animusInstance)
    {
        foreach (var state in animusInstance.States)
        {
            EvaluateTriggers(entity, state);
        }
    }

    private bool EvaluateConditions(Entity<AnimusComponent> entity, AnimusStateBase state)
    {
        foreach (var cond in state.Conditions)
        {
            // Skip the check if it was recently false.
            if ((!_timing.IsFirstTimePredicted || state.Instance.NextUpdate > _timing.CurTime) && !cond.LastResult)
                return false;

            if (!cond.EvaluateInternal(entity))
                return false;
        }
        return true;
    }

    private static void EvaluateTriggers(Entity<AnimusComponent> entity, AnimusStateBase state)
    {
        foreach (var trigger in state.Triggers)
        {
            if (trigger.TriggerIfNecessaryInternal(entity))
            {
                return;
            }
        }
    }

    private static void SwitchState(Entity<AnimusComponent> entity, AnimusStateBase newState, bool switchedByTrigger)
    {
        if (newState.Instance.ActiveState == newState)
            return;

        newState.Instance.ActiveState.Exit(entity.Owner);
        newState.Enter(entity.Owner, switchedByTrigger);
        newState.Instance.ActiveState = newState;
    }

    public void TriggerFor<TEvent>(Entity<AnimusComponent> entity) where TEvent : EntityEventArgs
    {
        if (!TryComp<AnimusComponent>(entity, out var comp))
            return;

        // Good thing this isn't called on a per-frame basis...
        foreach (var animusInstance in comp.ActiveStateMachines)
        {
            foreach (var state in animusInstance.States)
            {
                foreach (var trigger in state.Triggers)
                {
                    if (trigger is AnimusTriggerEvents eventsTrigger && eventsTrigger.Events.Contains(typeof(TEvent).Name))
                    {
                        TriggerInternal(state, entity);
                    }
                }
            }
        }
    }
}
