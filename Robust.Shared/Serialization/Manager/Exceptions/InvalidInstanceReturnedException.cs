using System;

namespace Robust.Shared.Serialization.Manager.Exceptions;

public sealed class InvalidInstanceReturnedException : Exception
{
    public readonly Type Expected;
    public readonly Type? Actual;
    public override string Message => $"Expected InstantiationDelegate to return value of type {Expected}, but {(Actual?.ToString() ?? "[NULLVALUE]")} was returned";

    public InvalidInstanceReturnedException(Type expected, Type? actual)
    {
        Expected = expected;
        Actual = actual;
    }
}
