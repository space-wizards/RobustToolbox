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
    /// (<see cref="RegisterWithCondition"/>, <see cref="RegisterForAssignableFrom{T}"/> and similar methods).
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
    /// Conditions will be checked if none of <see cref="RegisterForType{T}"/> registrations were fitting.
    /// </summary>
    /// <param name="factoryMethod">Factory method that creates VV control.</param>
    /// <param name="insertPosition">Where new condition should be inserted - at start or at the end of the list.</param>
    void RegisterForAssignableFrom<T>(Func<Type, VVPropEditor> factoryMethod, InsertPosition insertPosition = InsertPosition.First);

    /// <summary>
    /// Registers factory method for vv control. This factory method will be used if <paramref name="condition"/> will return true for provided type.
    /// Conditions will be checked if none of <see cref="RegisterForType{T}"/> registrations were fitting.
    /// </summary>
    /// <param name="condition">Condition, that will decide, if factory should be used for provided type.</param>
    /// <param name="factory">Factory method that creates VV control.</param>
    /// <param name="insertPosition">Where new condition should be inserted - at start or at the end of the list. </param>
    void RegisterWithCondition(Func<Type, bool> condition, Func<Type, VVPropEditor> factory, InsertPosition insertPosition = InsertPosition.First);
}

/// <summary>
/// Indicator, where item should be inserted in list.
/// </summary>
public enum InsertPosition
{
    /// <summary>
    /// Item will be inserted as first in list.
    /// </summary>
    First,
    /// <summary>
    /// Item will be inserted as last in list.
    /// </summary>
    Last
}
