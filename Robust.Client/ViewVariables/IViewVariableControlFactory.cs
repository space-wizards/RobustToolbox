using System;
using Robust.Client.ViewVariables.Editors;

namespace Robust.Client.ViewVariables;

/// <summary>
/// Factory that creates UI controls for viewing variables based on provided property type.
/// </summary>
public interface IViewVariableControlFactory
{
    /// <summary>
    /// Creates UI control for viewing variable of type <paramref name="type"/>.
    /// Returns <see cref="VVPropEditorDummy"/> if fails to find proper control.
    /// First will look for control factory by type match (<see cref="RegisterForType{T}"/>),
    /// then will try to check each registered conditional factory
    /// (<see cref="RegisterWithConditionAtStart"/>, <see cref="RegisterForAssignableFromAtStart{T}"/> and similar methods).
    /// </summary>
    VVPropEditor CreateFor(Type? type);

    /// <summary>
    /// Registers factory method for vv control. This factory method will be used if provided type will be exactly equal to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Type of property, for which this control factory should be used. Will only be used in case of exact match.</typeparam>
    /// <param name="factoryMethod">Factory method that creates VV control.</param>
    void RegisterForType<T>(Func<Type, VVPropEditor> factoryMethod);

    /// <summary>
    /// Registers factory method for vv control. This factory method will be used if provided type will be assignable to <typeparamref name="T"/>.
    /// Condition will be inserted into first position in list of conditions to check (after ones registered <see cref="RegisterForType{T}"/>).
    /// </summary>
    /// <param name="factoryMethod">Factory method that creates VV control.</param>
    void RegisterForAssignableFromAtStart<T>(Func<Type, VVPropEditor> factoryMethod);

    /// <summary>
    /// Registers factory method for vv control. This factory method will be used if provided type will be assignable to <typeparamref name="T"/>.
    /// Condition will be inserted into last position in list of conditions to check (after ones registered <see cref="RegisterForType{T}"/>).
    /// </summary>
    /// <param name="factoryMethod">Factory method that creates VV control.</param>
    void RegisterForAssignableFromAtEnd<T>(Func<Type, VVPropEditor> factoryMethod);

    /// <summary>
    /// Registers factory method for vv control. This factory method will be used if <paramref name="condition"/> will return true for provided type.
    /// Condition will be inserted into first position in list of conditions to check (after ones registered <see cref="RegisterForType{T}"/>).
    /// </summary>
    /// <param name="condition">Condition, that will decide, if factory should be used for provided type.</param>
    /// <param name="factory">Factory method that creates VV control.</param>
    void RegisterWithConditionAtStart(Func<Type, bool> condition, Func<Type, VVPropEditor> factory);

    /// <summary>
    /// Registers factory method for vv control. This factory method will be used if <paramref name="condition"/> will return true for provided type.
    /// Condition will be inserted into last position in list of conditions to check (after ones registered <see cref="RegisterForType{T}"/>).
    /// </summary>
    /// <param name="condition">Condition, that will decide, if factory should be used for provided type.</param>
    /// <param name="factory">Factory method that creates VV control.</param>
    void RegisterWithConditionAtEnd(Func<Type, bool> condition, Func<Type, VVPropEditor> factory);
}
