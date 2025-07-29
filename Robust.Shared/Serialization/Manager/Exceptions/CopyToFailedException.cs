using System;

namespace Robust.Shared.Serialization.Manager.Exceptions;

public sealed class CopyToFailedException<T> : Exception
{
    public override string Message => $"Failed performing CopyTo for Type {typeof(T)}";
}
