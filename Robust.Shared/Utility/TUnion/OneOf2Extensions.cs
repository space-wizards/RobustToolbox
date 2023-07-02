using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Robust.Shared.Log;

namespace Robust.Shared.Utility.TUnion;

public static class OneOf2ResultExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsError<T0, T1>(this IOneOf<T0, T1> val)
        where T0 : notnull
        where T1 : IError
    {
        return val.IsItem2;
    }

    public static T0 Expect<T0, T1>(this IOneOf<T0, T1> val)
        where T0 : notnull
        where T1 : IError
    {
        if (val.IsItem1)
        {
            return val.Item1OrErr;
        }

        throw new Exception(val.Item2OrErr.Describe());
    }

    public static T0? ReportErr<T0, T1>(
        this OneOfValue<T0, T1> val,
        ISawmill mill,
        [CallerLineNumber] int lineNo = 0,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = ""
    )
        where T0 : unmanaged
        where T1 : unmanaged, IError
    {
        if (val.IsItem2)
        {
            mill.Error($"Reported error at line {lineNo} in {memberName} at {filePath}:");
            mill.Error(val.Item2OrErr.Describe());
            return null;
        }

        return val.Item1OrErr;
    }

    public static T0? ReportErr<T0, T1>(
            this OneOf<T0, T1> val,
            ISawmill mill,
            [CallerLineNumber] int lineNo = 0,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = ""
        )
        where T0 : notnull
        where T1 : IError
    {
        if (val.IsItem2)
        {
            mill.Error($"Reported error at line {lineNo} in {memberName} at {filePath}:");
            mill.Error(val.Item2OrErr.Describe());
            return default;
        }

        return val.Item1OrErr;
    }

    public static OneOfValue<T0, T1> OrErr<T0, T1>(this T0? val, T1 err)
        where T0 : unmanaged
        where T1 : unmanaged, IError
    {
        if (val is not null)
            return new(val.Value);

        return new(err);
    }

    public static OneOf<T0, T1> OrErr<T0, T1>(this T0? val, T1 err)
        where T0 : notnull
        where T1 : IError
    {
        if (val is not null)
            return new(val);

        return new(err);
    }

    public static OneOf<T0, IError> Simplify<T0, T1>(this OneOfValue<T0, T1> val)
        where T0 : unmanaged
        where T1 : unmanaged, IError
    {
        return val switch
        {
            {Item1: { } item1} => new(item1),
            {Item2: { } item2} => new(item2),
            _ => throw new UnreachableException()
        };
    }

    public static OneOf<T0, IError> Simplify<T0, T1>(this OneOf<T0, T1> val)
        where T0 : unmanaged
        where T1 : unmanaged, IError
    {
        return val switch
        {
            {Item1: { } item1, IsItem2: false} => new(item1),
            {Item2: { } item2, IsItem1: false} => new(item2),
            _ => throw new UnreachableException()
        };
    }


}
