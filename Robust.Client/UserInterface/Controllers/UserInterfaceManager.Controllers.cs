using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Robust.Client.State;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Robust.Shared.Serialization.Manager.Definition.DataDefinition;

// ReSharper disable once CheckNamespace
namespace Robust.Client.UserInterface;

internal partial class UserInterfaceManager
{
    /// <summary>
    ///     All registered <see cref="UIController"/> instances indexed by type
    /// </summary>
    private readonly Dictionary<Type, UIController> _uiControllers = new();

    /// <summary>
    ///     Dependency collection holding UI controllers and IoC services
    /// </summary>
    private DependencyCollection _dependencies = default!;

    /// <summary>
    ///     Implementations of <see cref="IOnStateEntered{T}"/> to invoke when a state is entered
    ///     State Type -> (UIController, Caller)
    /// </summary>
    private readonly Dictionary<Type, Dictionary<UIController, StateChangedCaller>> _onStateEnteredDelegates = new();

    /// <summary>
    ///     Implementations of <see cref="IOnStateExited{T}"/> to invoke when a state is exited
    ///     State Type -> (UIController, Caller)
    /// </summary>
    private readonly Dictionary<Type, Dictionary<UIController, StateChangedCaller>> _onStateExitedDelegates = new();

    /// <summary>
    ///     Implementations of <see cref="IOnSystemLoaded{T}"/> to invoke when an entity system is loaded
    ///     Entity System Type -> (UIController, Caller)
    /// </summary>
    private readonly Dictionary<Type, Dictionary<UIController, SystemChangedCaller>> _onSystemLoadedDelegates = new();

    /// <summary>
    ///     Implementations of <see cref="IOnSystemUnloaded{T}"/> to invoke when an entity system is unloaded
    ///     Entity System Type -> (UIController, Caller)
    /// </summary>
    private readonly Dictionary<Type, Dictionary<UIController, SystemChangedCaller>> _onSystemUnloadedDelegates = new();

    /// <summary>
    ///     Field -> Controller -> Field assigner delegate
    /// </summary>
    private readonly Dictionary<Type, Dictionary<Type, AssignField<UIController, object?>>> _assignerRegistry = new();

    private delegate void StateChangedCaller(object controller, State.State state);

    private delegate void SystemChangedCaller(object controller, IEntitySystem system);

    private StateChangedCaller EmitStateChangedCaller(Type controller, Type state, bool entered)
    {
        if (controller.IsValueType)
        {
            throw new ArgumentException($"Value type controllers are not supported. Controller: {controller}");
        }

        if (state.IsValueType)
        {
            throw new ArgumentException($"Value type states are not supported. State: {state}");
        }

        var method = new DynamicMethod(
            "StateChangedCaller",
            typeof(void),
            new[] {typeof(object), typeof(State.State)},
            true
        );

        var generator = method.GetILGenerator();

        Type onStateChangedType;
        MethodInfo onStateChangedMethod;
        if (entered)
        {
            onStateChangedType = typeof(IOnStateEntered<>).MakeGenericType(state);
            onStateChangedMethod =
                controller.GetMethod(nameof(IOnStateEntered<State.State>.OnStateEntered), new[] {state})
                ?? throw new NullReferenceException();
        }
        else
        {
            onStateChangedType = typeof(IOnStateExited<>).MakeGenericType(state);
            onStateChangedMethod =
                controller.GetMethod(nameof(IOnStateExited<State.State>.OnStateExited), new[] {state})
                ?? throw new NullReferenceException();
        }

        generator.Emit(OpCodes.Ldarg_0); // controller
        generator.Emit(OpCodes.Castclass, onStateChangedType);

        generator.Emit(OpCodes.Ldarg_1); // state
        generator.Emit(OpCodes.Castclass, state);

        generator.Emit(OpCodes.Callvirt, onStateChangedMethod);
        generator.Emit(OpCodes.Ret);

        return method.CreateDelegate<StateChangedCaller>();
    }

    private SystemChangedCaller EmitSystemChangedCaller(Type controller, Type system, bool loaded)
    {
        if (controller.IsValueType)
        {
            throw new ArgumentException($"Value type controllers are not supported. Controller: {controller}");
        }

        if (system.IsValueType)
        {
            throw new ArgumentException($"Value type systems are not supported. System: {system}");
        }

        var method = new DynamicMethod(
            "SystemChangedCaller",
            typeof(void),
            new[] {typeof(object), typeof(IEntitySystem)},
            true
        );

        var generator = method.GetILGenerator();

        Type onSystemChangedType;
        MethodInfo onSystemChangedMethod;
        if (loaded)
        {
            onSystemChangedType = typeof(IOnSystemLoaded<>).MakeGenericType(system);
            onSystemChangedMethod =
                controller.GetMethod(nameof(IOnSystemLoaded<IEntitySystem>.OnSystemLoaded), new[] {system})
                ?? throw new NullReferenceException();
        }
        else
        {
            onSystemChangedType = typeof(IOnSystemUnloaded<>).MakeGenericType(system);
            onSystemChangedMethod =
                controller.GetMethod(nameof(IOnSystemUnloaded<IEntitySystem>.OnSystemUnloaded), new[] {system})
                ?? throw new NullReferenceException();
        }

        generator.Emit(OpCodes.Ldarg_0); // controller
        generator.Emit(OpCodes.Castclass, onSystemChangedType);

        generator.Emit(OpCodes.Ldarg_1); // system
        generator.Emit(OpCodes.Castclass, system);

        generator.Emit(OpCodes.Callvirt, onSystemChangedMethod);
        generator.Emit(OpCodes.Ret);

        return method.CreateDelegate<SystemChangedCaller>();
    }

    private ref UIController GetUIControllerRef(Type type)
    {
        return ref CollectionsMarshal.GetValueRefOrNullRef(_uiControllers, type);
    }

    private UIController GetUIController(Type type)
    {
        return _uiControllers[type];
    }

    public T GetUIController<T>() where T : UIController, new()
    {
        return (T) GetUIController(typeof(T));
    }

    private void SetupControllers()
    {
        foreach (var uiControllerType in _reflectionManager.GetAllChildren<UIController>())
        {
            if (uiControllerType.IsAbstract)
                continue;

            _dependencies.Register(uiControllerType);
        }

        _dependencies.BuildGraph();

        foreach (var controllerType in _reflectionManager.GetAllChildren<UIController>())
        {
            var controller = (UIController) _dependencies.ResolveType(controllerType);
            _uiControllers[controllerType] = controller;

            foreach (var fieldInfo in controllerType.GetAllPropertiesAndFields())
            {
                if (!fieldInfo.HasAttribute<UISystemDependencyAttribute>())
                {
                    continue;
                }

                var backingField = fieldInfo;
                if (fieldInfo is SpecificPropertyInfo property)
                {
                    if (property.TryGetBackingField(out var field))
                    {
                        backingField = field;
                    }
                    else
                    {
                        var setter = property.PropertyInfo.GetSetMethod(true);
                        if (setter == null)
                        {
                            throw new InvalidOperationException(
                                $"Property with {nameof(UISystemDependencyAttribute)} attribute did not have a backing field nor setter");
                        }
                    }
                }

                //Do not do anything if the field isn't an entity system
                if (!backingField.FieldType.IsAssignableTo(typeof(IEntitySystem)))
                    continue;

                var typeDict = _assignerRegistry.GetOrNew(fieldInfo.FieldType);
                var assigner = EmitFieldAssigner<UIController>(controllerType, fieldInfo.FieldType, backingField);
                typeDict.Add(controllerType, assigner);
            }

            foreach (var @interface in controllerType.GetInterfaces())
            {
                if (!@interface.IsGenericType)
                    continue;

                var typeDefinition = @interface.GetGenericTypeDefinition();
                var genericType = @interface.GetGenericArguments()[0];
                if (typeDefinition == typeof(IOnStateEntered<>))
                {
                    var enteredCaller = EmitStateChangedCaller(controllerType, genericType, true);
                    _onStateEnteredDelegates.GetOrNew(genericType).Add(controller, enteredCaller);
                }
                else if (typeDefinition == typeof(IOnStateExited<>))
                {
                    var exitedCaller = EmitStateChangedCaller(controllerType, genericType, false);
                    _onStateExitedDelegates.GetOrNew(genericType).Add(controller, exitedCaller);
                }
                else if (typeDefinition == typeof(IOnSystemLoaded<>))
                {
                    var loadedCaller = EmitSystemChangedCaller(controllerType, genericType, true);
                    _onSystemLoadedDelegates.GetOrNew(genericType).Add(controller, loadedCaller);
                }
                else if (typeDefinition == typeof(IOnSystemUnloaded<>))
                {
                    var unloadedCaller = EmitSystemChangedCaller(controllerType, genericType, false);
                    _onSystemUnloadedDelegates.GetOrNew(genericType).Add(controller, unloadedCaller);
                }
            }
        }

        _systemManager.SystemLoaded += OnSystemLoaded;
        _systemManager.SystemUnloaded += OnSystemUnloaded;

        _stateManager.OnStateChanged += OnStateChanged;

        _dependencies.BuildGraph();
    }

    private void InitializeControllers()
    {
        foreach (var controller in _uiControllers.Values)
        {
            controller.Initialize();
        }
    }

    private void UpdateControllers(FrameEventArgs args)
    {
        foreach (var controller in _uiControllers.Values)
        {
            // TODO hud refactor run frame update only if it has been overriden
            controller.FrameUpdate(args);
        }
    }

    // TODO hud refactor optimize this to use an array
    // TODO hud refactor BEFORE MERGE cleanup subscriptions for all implementations when switching out of gameplay state
    private void OnStateChanged(StateChangedEventArgs args)
    {
        if (_onStateExitedDelegates.TryGetValue(args.OldState.GetType(), out var exitedDelegates))
        {
            foreach (var (controller, caller) in exitedDelegates)
            {
                caller(controller, args.OldState);
            }
        }

        if (_onStateEnteredDelegates.TryGetValue(args.NewState.GetType(), out var enteredDelegates))
        {
            foreach (var (controller, caller) in enteredDelegates)
            {
                caller(controller, args.NewState);
            }
        }
    }

    private void OnSystemLoaded(object? sender, SystemChangedArgs args)
    {
        var systemType = args.System.GetType();

        if (_assignerRegistry.TryGetValue(systemType, out var assigners))
        {
            foreach (var (controllerType, assigner) in assigners)
            {
                assigner(ref GetUIControllerRef(controllerType), args.System);
            }
        }

        if (_onSystemLoadedDelegates.TryGetValue(systemType, out var delegates))
        {
            foreach (var (controller, caller) in delegates)
            {
                caller(controller, args.System);
            }
        }
    }

    private void OnSystemUnloaded(object? system, SystemChangedArgs args)
    {
        var systemType = args.System.GetType();

        if (_onSystemUnloadedDelegates.TryGetValue(systemType, out var delegates))
        {
            foreach (var (controller, caller) in delegates)
            {
                caller(controller, args.System);
            }
        }

        if (_assignerRegistry.TryGetValue(systemType, out var assigners))
        {
            foreach (var (controllerType, assigner) in assigners)
            {
                assigner(ref GetUIControllerRef(controllerType), null);
            }
        }
    }
}
