using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;

namespace Robust.Shared.Utility.TUnion;

public interface IError
{
    [MustUseReturnValue]
    public string Describe();

    [MustUseReturnValue]
    public virtual IError? MergeWith(IError right)
    {
        return null;
    }

    [MustUseReturnValue]
    public static IError operator +(IError? left, IError right)
    {
        if (left is null)
            return right;

        if (left.MergeWith(right) is { } res)
        {
            return res;
        }

        return new AggregateError(left, right);
    }
}

public sealed class AggregateError : IError
{
    private readonly List<IError> _errors;

    public AggregateError(IError left, IError right)
    {
        _errors = new List<IError>() {left, right};
    }

    public static IError operator +(AggregateError? left, IError right)
    {
        if (left is null)
            return right;

        left._errors.Add(right);
        return left;
    }

    public IError? MergeWith(IError right)
    {
        return this + right;
    }

    public string Describe()
    {
        var builder = new StringBuilder("Ran into the following problems:");

        foreach (var e in _errors)
        {
            builder.AppendLine($"- {e.Describe()}");
        }

        return builder.ToString();
    }
}
