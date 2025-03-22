using System.Linq;
using NUnit.Framework.Constraints;
using Robust.Shared.GameObjects;

namespace Robust.UnitTesting.Constraints;

public sealed class EntityComponentConstraint<T>(IEntityManager entMan) : Constraint
    where T : IComponent, new()
{
    private readonly IEntityManager _entMan = entMan;

    public override string Description => _entMan.ComponentFactory.GetComponentName<T>();

    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        var uid = ConstraintUtils.GetEntityUid(actual);
        var components = _entMan.GetComponents(uid).Select(c => _entMan.ComponentFactory.GetComponentName(c.GetType()))
            .HighlightMatches(_entMan.ComponentFactory.GetComponentName<T>());
        return new ConstraintResult(this, components, _entMan.HasComponent<T>(uid));
    }
}
