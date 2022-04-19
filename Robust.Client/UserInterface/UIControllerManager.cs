using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Robust.Client.State;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface;

[Virtual]
public abstract class UIController
{
    public virtual void OnSystemLoaded(IEntitySystem system) {}
    public virtual void OnSystemUnloaded(IEntitySystem system) {}
}

//notices your IUI, *UwU What's this?*
public interface IUIControllerManager
{
    public T GetController<T>() where T : UIController, new();
}

internal interface IUIControllerManagerInternal : IUIControllerManager
{
    void Initialize();
}

public sealed class UISystemDependency : Attribute {}

internal sealed class UIControllerManager : IUIControllerManagerInternal
{
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;
    [Dependency] private readonly IDynamicTypeFactoryInternal _dynamicTypeFactory = default!;
    [Dependency] private readonly IEntitySystemManager _systemManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;

    private readonly Dictionary<Type, List<UIController>> _systemUnloadedListeners = new();
    private readonly Dictionary<Type, List<UIController>> _systemLoadedListeners = new();
    private UIController[] _uiControllerRegistry = default!;
    private readonly Dictionary<Type, int> _uiControllerIndices = new();
    private readonly Dictionary<Type, List<object>> _onStateChanged = new();
    private readonly Dictionary<(Type controller, Type state), StateChangedCaller> _onStateChangedCallers = new();

    //field type, systemsDict
    private readonly Dictionary<Type, List<(Type, DataDefinition.AssignField<UIController,object?>)>> _assignerRegistry = new ();

    private delegate void StateChangedCaller(object controller, State.State state);

    private StateChangedCaller EmitStateChangedCaller(Type controller, Type state)
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
        var onStateChangedType = typeof(IOnStateChanged<>).MakeGenericType(state);
        var onStateChangedMethod =
            controller.GetMethod(nameof(IOnStateChanged<State.State>.OnStateChanged), new[] {state})
            ?? throw new NullReferenceException();

        generator.Emit(OpCodes.Ldarg_0); // controller
        generator.Emit(OpCodes.Castclass, onStateChangedType);

        generator.Emit(OpCodes.Ldarg_1); // state
        generator.Emit(OpCodes.Castclass, state);

        generator.Emit(OpCodes.Callvirt, onStateChangedMethod);
        generator.Emit(OpCodes.Ret);

        return method.CreateDelegate<StateChangedCaller>();
    }

    // TODO hud refactor BEFORE MERGE optimize this to use an array
    // TODO hud refactor BEFORE MERGE cleanup subscriptions for all implementations when switching out of gameplay state
    private void OnStateChanged(StateChangedEventArgs args)
    {
        var stateType = args.NewState.GetType();
        foreach (var controller in _onStateChanged[stateType])
        {
            var controllerType = controller.GetType();
            _onStateChangedCallers[(controllerType, stateType)](controller, args.NewState);
        }
    }

    private void AddUiControllerToRegistry(List<UIController> list,Type type, UIController instance)
    {
        list.Add(instance);
        _uiControllerIndices.Add(type, list.Count - 1);
    }

    private ref UIController GetUiControllerByTypeRef(Type type)
    {
        return ref _uiControllerRegistry[_uiControllerIndices[type]];
    }

    private UIController GetUiControllerByType(Type type)
    {
        return _uiControllerRegistry[_uiControllerIndices[type]];
    }

    private T GetUiControllerByType<T>() where T : UIController, new()
    {
        return (T)_uiControllerRegistry[_uiControllerIndices[typeof(T)]];
    }

    public T GetController<T>() where T : UIController, new()
    {
        return GetUiControllerByType<T>();
    }

    public void Initialize()
    {
        foreach (var state in _reflectionManager.GetAllChildren<State.State>())
        {
            _onStateChanged.Add(state, new List<object>());
        }

        List<UIController> tempList = new();
        foreach (var uiControllerType in _reflectionManager.GetAllChildren<UIController>())
        {
            if (uiControllerType.IsAbstract)
                continue;

            var newController = _dynamicTypeFactory.CreateInstanceUnchecked<UIController>(uiControllerType);

            // TODO hud refactor BEFORE MERGE add them all at the end to prevent people from being too smart
            AddUiControllerToRegistry(tempList, uiControllerType, newController);

            foreach (var fieldInfo in uiControllerType.GetAllPropertiesAndFields())
            {
                if (!fieldInfo.HasAttribute<UISystemDependency>())
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
                            throw new InvalidOperationException($"Property with {nameof(UISystemDependency)} attribute did not have a backing field nor setter");
                        }
                    }
                }

                //Do not do anything if the field isn't an entity system
                if (!typeof(IEntitySystem).IsAssignableFrom(backingField.FieldType))
                    continue;

                var typeDict = _assignerRegistry.GetOrNew(fieldInfo.FieldType);
                typeDict.Add((uiControllerType,DataDefinition.EmitFieldAssigner<UIController>(uiControllerType, fieldInfo.FieldType, backingField)));

                if (uiControllerType.GetMethod(nameof(UIController.OnSystemLoaded))?.DeclaringType != typeof(UIController))
                {
                    _systemLoadedListeners.GetOrNew(fieldInfo.FieldType).Add(newController);
                }

                if (uiControllerType.GetMethod(nameof(UIController.OnSystemUnloaded))?.DeclaringType != typeof(UIController))
                {
                    _systemUnloadedListeners.GetOrNew(fieldInfo.FieldType).Add(newController);
                }
            }

            foreach (var @interface in uiControllerType.GetInterfaces())
            {
                if (@interface.GetGenericTypeDefinition() != typeof(IOnStateChanged<>))
                    continue;

                var stateType = @interface.GetGenericArguments()[0];
                _onStateChanged[stateType].Add(newController);

                var caller = EmitStateChangedCaller(uiControllerType, stateType);
                _onStateChangedCallers.Add((uiControllerType, stateType), caller);
            }
        }

        _uiControllerRegistry = tempList.ToArray();

        _systemManager.SystemLoaded += OnSystemLoaded;
        _systemManager.SystemUnloaded += OnSystemUnloaded;

        _stateManager.OnStateChanged += OnStateChanged;
    }

    private void OnSystemLoaded(object? sender,SystemChangedArgs args)
    {
        if (!_assignerRegistry.TryGetValue(args.System.GetType(), out var fieldData)) return;
        foreach (var (controllerType, accessor) in fieldData)
        {
            accessor( ref GetUiControllerByTypeRef(controllerType),args.System);
        }

        if (!_systemLoadedListeners.TryGetValue(args.System.GetType(), out var controllers)) return;
        foreach (var uiCon in controllers)
        {
            uiCon.OnSystemLoaded(args.System);
        }
    }

    private void OnSystemUnloaded(object? system,SystemChangedArgs args)
    {
        var systemType = args.System.GetType();

        if (_systemUnloadedListeners.TryGetValue(systemType, out var controllers))
        {
            foreach (var uiCon in controllers)
            {
                uiCon.OnSystemUnloaded(args.System);
            }
        }

        if (!_assignerRegistry.TryGetValue(systemType, out var fieldData)) return;
        foreach (var (controllerType, accessor) in fieldData)
        {
            accessor(ref GetUiControllerByTypeRef(controllerType), null);
        }
    }
}
