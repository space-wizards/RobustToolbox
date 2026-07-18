using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.Animus.States;
using Robust.Client.GameObjects;
using Robust.Client.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Robust.Client.Animus;

public sealed partial class AnimusSystem : EntitySystem
{
    [Dependency] private ILogManager _logger = default!;
    [Dependency] private IClientGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private ISerializationManager _serializationManager = default!;

    private ISawmill _sawmill = default!;

    private readonly Dictionary<EntityUid, List<(Type, string)>> _actingComponentProperties = new();
    private readonly Dictionary<EntityUid, List<string>> _runningAnimations = new();

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logger.GetSawmill("asm");

        SubscribeLocalEvent<AnimusComponent, ComponentInit>(OnAnimationStateMachineComponentInit);
        SubscribeLocalEvent<AnimationPlayerComponent, AnimationCompletedEvent>(OnAnimationCompleted);
        SubscribeLocalEvent<AnimationPlayerComponent, AnimationStartedEvent>(OnAnimationStarted);
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

    private void OnAnimationStarted(Entity<AnimationPlayerComponent> entity, ref AnimationStartedEvent args)
    {
        if (!TryComp<AnimusComponent>(entity, out var comp))
            return;

        if (!_runningAnimations.ContainsKey(entity.Owner))
            _runningAnimations.Add(entity.Owner, []);

        _runningAnimations[entity.Owner].Add(args.Key);
    }

    private void OnAnimationCompleted(Entity<AnimationPlayerComponent> entity, ref AnimationCompletedEvent args)
    {
        if (!TryComp<AnimusComponent>(entity, out var comp))
            return;

        if(_runningAnimations.TryGetValue(entity.Owner, out var animKeys))
            animKeys.Remove(args.Key);

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
            SwitchState((entity, comp), animusInstance.DefaultState);
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

        return stateCopy;
    }

    private void UpdateStateMachine(Entity<AnimusComponent> entity, AnimusInstance animusInstance)
    {
        ExitActiveStateIfExitTimerReached(entity, animusInstance);
        CheckStateMachineConditionsAndUpdateState(entity, animusInstance);
    }

    private void ExitActiveStateIfExitTimerReached(Entity<AnimusComponent> entity, AnimusInstance animusInstance)
    {
        if (animusInstance.ActiveStateExitTime == TimeSpan.Zero)
            return;

        if (animusInstance.ActiveStateExitTime > _timing.CurTime)
            return;

        SwitchState(entity, animusInstance.DefaultState);
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

        SwitchState(entity, nextState);
        if (nextState.ExitPeriod != TimeSpan.Zero)
            animusInstance.ActiveStateExitTime = _timing.CurTime + nextState.ExitPeriod;
    }

    private bool EvaluateConditions(Entity<AnimusComponent> entity, AnimusStateBase state)
    {
        foreach (var cond in state.Conditions)
        {
            if (!cond.EvaluateInternal(entity, _timing.CurTime, _timing.IsFirstTimePredicted))
                return false;
        }
        return true;
    }

    private static void SwitchState(Entity<AnimusComponent> entity, AnimusStateBase newState)
    {
        if (newState.Instance.ActiveState == newState)
            return;

        newState.Instance.ActiveState.Exit(entity.Owner);
        newState.Enter(entity.Owner);
        newState.Instance.ActiveState = newState;
    }

    internal bool HasAnimationRunning(EntityUid entityUid, string key)
    {
        return _runningAnimations.ContainsKey(entityUid) && _runningAnimations[entityUid].Contains(key);
    }
}
