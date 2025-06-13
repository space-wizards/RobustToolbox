using System.Linq;
using NUnit.Framework.Constraints;
using Robust.Shared.GameObjects;

namespace Robust.UnitTesting.Constraints;

public sealed class EntityManagerComponentCountConstraint<T>(IConstraint baseConstraint) : PrefixConstraint(baseConstraint, "component count")
    where T : IComponent
{
    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        var entMan = ConstraintUtils.RequireActual<IEntityManager>(actual);
        var allComps = entMan.AllComponents<T>();
        var entsWithComp = allComps.Select(e => entMan.ToPrettyString(e.Uid));
        var baseResult = BaseConstraint.ApplyTo(allComps.Length);
        return new EntityManagerComponentCountConstraintResult(this, baseResult);
    }

    protected override string GetStringRepresentation()
    {
        return $"<component count {BaseConstraint}>";
    }
}

internal sealed class EntityManagerComponentCountConstraintResult(IConstraint constraint, ConstraintResult baseResult)
    : ConstraintResult(constraint, baseResult.ActualValue, baseResult.Status)
{
    private readonly ConstraintResult _baseResult = baseResult;

    public override void WriteAdditionalLinesTo(MessageWriter writer)
    {
        _baseResult.WriteAdditionalLinesTo(writer);
    }
}
