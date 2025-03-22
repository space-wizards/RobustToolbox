using NUnit.Framework.Constraints;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Robust.UnitTesting.Constraints;

public sealed class EntityPrototypeComponentConstraint<T>(IComponentFactory compFactory) : Constraint
    where T : IComponent, new()
{
    private readonly IComponentFactory _compFactory = compFactory;
    public override string Description => _compFactory.GetComponentName<T>();

    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        var entProto = ConstraintUtils.RequireActual<EntityPrototype>(actual);
        // List all components defined on the prototype, highlighting any that match (for use with Not constraint)
        var components = entProto.Components.Keys.HighlightMatches(_compFactory.GetComponentName<T>());
        // Identify the protoId in the result message to help with debugging
        return new ConstraintResult(this, $"{entProto.ID}: < {string.Join(", ", components)} >", entProto.TryGetComponent<T>(out _, _compFactory));
    }
}
