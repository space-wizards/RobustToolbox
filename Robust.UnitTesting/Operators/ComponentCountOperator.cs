using NUnit.Framework.Constraints;
using Robust.Shared.GameObjects;
using Robust.UnitTesting.Constraints;

namespace Robust.UnitTesting.Operators;

public sealed class ComponentCountOperator<T> : SelfResolvingOperator
    where T : IComponent
{

    public override void Reduce(ConstraintBuilder.ConstraintStack stack)
    {
        stack.Push(new EntityManagerComponentCountConstraint<T>(stack.Pop()));
    }
}
