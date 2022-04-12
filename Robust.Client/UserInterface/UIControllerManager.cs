using System;
using System.Collections.Generic;
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
    // TODO hud refactor BEFORE MERGE cleanup subscriptions for all implementations when switching out of gameplay state
    public virtual void OnStateChanged(StateChangedEventArgs args) {}

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

internal sealed class UIControllerManager: IUIControllerManagerInternal
{
    [Dependency] private readonly IReflectionManager _reflectionManager = default!;
    [Dependency] private readonly IDynamicTypeFactoryInternal _dynamicTypeFactory = default!;
    [Dependency] private readonly IEntitySystemManager _systemManager = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;

    private readonly Dictionary<Type, List<UIController>> _systemUnloadedListeners = new();
    private readonly Dictionary<Type, List<UIController>> _systemLoadedListeners = new();
    private UIController[] _uiControllerRegistry = default!;
    private readonly Dictionary<Type, int> _uiControllerIndices = new();

    //field type, systemsDict
    private readonly Dictionary<Type, List<(Type, DataDefinition.AssignField<UIController,object?>)>> _assignerRegistry = new ();

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

    private T GetUiControllerByType<T>() where T: UIController, new()
    {
        return (T)_uiControllerRegistry[_uiControllerIndices[typeof(T)]];
    }

    public T GetController<T>() where T : UIController, new()
    {
        return GetUiControllerByType<T>();
    }

    public void Initialize()
    {
        List<UIController> tempList = new();
        foreach (var uiControllerType in _reflectionManager.GetAllChildren<UIController>())
        {
            if (uiControllerType.IsAbstract) continue;
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
                if (!typeof(IEntitySystem).IsAssignableFrom(backingField.FieldType)) continue;
                var typeDict = _assignerRegistry.GetOrNew(fieldInfo.FieldType);
                typeDict.Add((uiControllerType,DataDefinition.EmitFieldAssigner<UIController>(uiControllerType, fieldInfo.FieldType, backingField)));

                if (uiControllerType.GetMethod(nameof(UIController.OnSystemLoaded))?.DeclaringType != typeof(UIController))
                {
                    _systemLoadedListeners.GetOrNew(fieldInfo.FieldType).Add((UIController)newController);
                }

                if (uiControllerType.GetMethod(nameof(UIController.OnSystemUnloaded))?.DeclaringType != typeof(UIController))
                {
                    _systemUnloadedListeners.GetOrNew(fieldInfo.FieldType).Add((UIController)newController);
                }
            }

            if (uiControllerType.GetMethod(nameof(UIController.OnStateChanged))?.DeclaringType != typeof(UIController))
                _stateManager.OnStateChanged += ((UIController)newController).OnStateChanged;


        }

        _uiControllerRegistry = tempList.ToArray();

        _systemManager.SystemLoaded += OnSystemLoaded;
        _systemManager.SystemUnloaded += OnSystemUnloaded;
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
