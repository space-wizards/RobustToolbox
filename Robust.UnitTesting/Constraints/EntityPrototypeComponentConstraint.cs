using System;
using System.Linq;
using NUnit.Framework.Constraints;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Robust.UnitTesting.Constraints;

public sealed class EntityPrototypeComponentConstraint<T>(IComponentFactory compFactory) : Constraint
    where T : IComponent, new()
{
    private readonly IComponentFactory _compFactory = compFactory;
    public override string Description => $"found component of type {typeof(T).Name}";

    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        if (actual is not EntityPrototype entProto)
            throw new ArgumentException($"Expected EntityPrototype but was {actual?.GetType()}");

        return new ConstraintResult(this, entProto.Components.Keys.ToArray(), entProto.TryGetComponent<T>(out _, _compFactory));
    }
}
