using System;
using NUnit.Framework.Constraints;
using Robust.Shared.Prototypes;

namespace Robust.UnitTesting.Constraints;

public sealed class PrototypeManagerIndexConstraint<T> : Constraint
    where T : class, IPrototype
{
    private readonly string _protoId;
    public override string Description => $"\"{_protoId}\" ({typeof(T).Name})";

    public PrototypeManagerIndexConstraint(ProtoId<T> protoId)
    {
        _protoId = protoId;
    }

    public PrototypeManagerIndexConstraint(string protoId)
    {
        _protoId = protoId;
    }

    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        if (actual is not IPrototypeManager protoMan)
            throw new ArgumentException($"Expected IPrototypeManager but was {actual?.GetType()}");

        // We don't use string.Join because some kinds (EntityPrototype) have a zillion instances.
        // The default writer only prints a portion of the list, which is fine.
        return new ConstraintResult(this, protoMan.GetInstances<T>().Keys, protoMan.HasIndex<T>(_protoId));
    }
}
