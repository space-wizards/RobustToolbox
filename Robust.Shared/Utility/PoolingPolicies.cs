using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace Robust.Shared.Utility;


public sealed class SetPolicy<T> : PooledObjectPolicy<HashSet<T>>
{
    public override HashSet<T> Create()
    {
        return new HashSet<T>();
    }

    public override bool Return(HashSet<T> obj)
    {
        obj.Clear();
        return true;
    }
}

public sealed class ListPolicy<T> : PooledObjectPolicy<List<T>>
{
    public override List<T> Create()
    {
        return new List<T>();
    }

    public override bool Return(List<T> obj)
    {
        obj.Clear();
        return true;
    }
}

public sealed class DictPolicy<T1, T2> : PooledObjectPolicy<Dictionary<T1, T2>> where T1 : notnull
{
    public override Dictionary<T1, T2> Create()
    {
        return new Dictionary<T1, T2>();
    }

    public override bool Return(Dictionary<T1, T2> obj)
    {
        obj.Clear();
        return true;
    }
}

public sealed class StackPolicy<T> : PooledObjectPolicy<Stack<T>>
{
    public override Stack<T> Create()
    {
        return new Stack<T>();
    }

    public override bool Return(Stack<T> obj)
    {
        obj.Clear();
        return true;
    }
}
