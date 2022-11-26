using System;
using Robust.Shared.Serialization.Markdown.Validation;

namespace Robust.Shared.Serialization.Manager.Exceptions;

public sealed class InvalidValidationNodeReturnedException<T> : Exception where T : ValidationNode
{
    public InvalidValidationNodeReturnedException(ValidationNode validationNode)
    {
        ActualType = validationNode.GetType();
    }

    public override string Message => $"{nameof(ValidationNode)} of type {ActualType} provided, but {typeof(T)} expected.";

    public Type ActualType;
}
