using System;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.Manager.Exceptions;

public sealed class CopyToFailedException<T> : Exception
{
    public override string Message
        => $"Failed performing CopyTo for Type {typeof(T)}. Did you forget to create a {nameof(ITypeCopier<T>)} implementation? Or maybe {typeof(T)} should have the {nameof(CopyByRefAttribute)}?";
}
