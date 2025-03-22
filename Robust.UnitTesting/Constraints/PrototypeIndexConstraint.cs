using System;
using NUnit.Framework.Constraints;
using Robust.Shared.Prototypes;

namespace Robust.UnitTesting.Constraints;

public sealed class PrototypeIndexConstraint<T> : Constraint
    where T : class, IPrototype
{
    private readonly string _protoId;
    public override string Description  => $"Found {typeof(T).Name} with ID {_protoId}";

    public PrototypeIndexConstraint(ProtoId<T> protoId)
    {
        _protoId = protoId;
    }

    public PrototypeIndexConstraint(string protoId)
    {
        _protoId = protoId;
    }

    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        if (actual is not IPrototypeManager protoMan)
            throw new ArgumentException($"Expected IPrototypeManager but was {actual?.GetType()}");

        return new Result(this, actual, protoMan.HasIndex(_protoId));
    }

    public sealed class Result(IConstraint constraint, object? actualValue, bool isSuccess) : ConstraintResult(constraint, actualValue, isSuccess)
    {
        public override void WriteActualValueTo(MessageWriter writer)
        {
            writer.Write("Failure");
        }
    }
}
