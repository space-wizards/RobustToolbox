using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Robust.Client.GameStates;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Sandboxing;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Utility;


namespace Robust.Client.UserInterface;

[Virtual]
public abstract class UIController
{
    public virtual void OnGamestateChanged(GameStateAppliedArgs args) {}

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
    [Robust.Shared.IoC.Dependency] private readonly IReflectionManager _reflectionManager = default!;
    [Robust.Shared.IoC.Dependency] private readonly IDynamicTypeFactory _dynamicTypeFactory = default!;
    [Robust.Shared.IoC.Dependency] private readonly IEntitySystemManager _systemManager = default!;
    [Robust.Shared.IoC.Dependency] private readonly IClientGameStateManager _gameStateManager = default!;
    private readonly Dictionary<Type, UIController> _registeredControllers = new();
    //field type, systemsDict
    private readonly Dictionary<Type, List<(Type,DataDefinition.AssignField<object,object?>)>> _assignerRegistry = new ();
    public UIControllerManager()
    {
        IoCManager.InjectDependencies(this);
    }
    public T GetController<T>() where T : UIController, new()
    {
        return (T)_registeredControllers[typeof(T)];
    }
    public void Initialize()
    {
        foreach (var uiControllerType in _reflectionManager.GetAllChildren<UIController>())
        {
            if (uiControllerType.IsAbstract) continue;
            var newController = _dynamicTypeFactory.CreateInstance(uiControllerType);
            _registeredControllers.Add(uiControllerType, (UIController)newController);
            foreach (var fieldInfo in uiControllerType.GetAllPropertiesAndFields())
            {
                var typeDict = _assignerRegistry.GetOrNew(fieldInfo.FieldType);
                typeDict.Add((uiControllerType,DataDefinition.EmitFieldAssigner(fieldInfo.FieldType, fieldInfo)));
            }
            if (uiControllerType.GetMethod(nameof(UIController.OnGamestateChanged))?.DeclaringType != typeof(UIController))
                _gameStateManager.GameStateApplied += ((UIController)newController).OnGamestateChanged;
        }

        _systemManager.SystemLoaded += OnSystemLoaded;
        _systemManager.SystemUnloaded += OnSystemUnloaded;
    }


    private void OnSystemLoaded(object? sender,SystemChangedArgs args)
    {
        if (!_assignerRegistry.TryGetValue(args.System.GetType(), out var fieldData)) return;
        foreach (var (controllerType, accessor) in fieldData)
        {//This may cause everything to catch fire and explode
            accessor(ref Unsafe.As<UIController,object>(ref CollectionsMarshal.GetValueRefOrNullRef(_registeredControllers, controllerType)), args.System);
        }
    }

    private void OnSystemUnloaded(object? system,SystemChangedArgs args)
    {
        if (!_assignerRegistry.TryGetValue(args.System.GetType(), out var fieldData)) return;
        foreach (var (controllerType, accessor) in fieldData)
        {
            accessor(ref Unsafe.As<UIController,object>(ref CollectionsMarshal.GetValueRefOrNullRef(_registeredControllers, controllerType)), null);
        }
    }


}
