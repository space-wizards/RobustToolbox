using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

namespace Robust.Shared.ViewVariables;

/*
 * TODO List!
 * - Decide how to handle specifiers with new type cache
 * - Clean up the whole entitymanager in vvpath mess
 * - Frankly just allow type caches to modify further paths lol shrimple!
 */

internal abstract partial class ViewVariablesManager
{
    [ViewVariables] private readonly Dictionary<Type, ViewVariablesTypeCache> _typeCache = new();

    private ViewVariablesTypeCache GetCache<T>()
    {
        return GetCache(typeof(T));
    }

    private ViewVariablesTypeCache GetCache(Type type)
    {
        if (_typeCache.TryGetValue(type, out var cached))
            return cached;

        var handler = (ViewVariablesTypeHandler)Activator.CreateInstance(typeof(ViewVariablesTypeHandler<>).MakeGenericType(type), true)!;
        var cache = new ViewVariablesTypeCache(type, handler);

        RepopulateCache(cache);

        _typeCache[type] = cache;

        return cache;
    }

    private void RepopulateCache(ViewVariablesTypeCache cache)
    {
        const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        var type = cache.Type;

        cache.Members.Clear();
        cache.InheritedMembers.Clear();
        cache.Indexers.Clear();

        foreach (var memberInfo in type.GetMembers(bindingFlags))
        {
            if (!ViewVariablesUtility.TryGetViewVariablesAccess(memberInfo, out var access))
                continue;

            // Skip indexers for now.
            if (memberInfo is PropertyInfo propertyInfo && propertyInfo.GetIndexParameters().Length != 0)
                continue;

            var member = new ViewVariablesTypeCache.Member(memberInfo);

            if (memberInfo.DeclaringType is not {} declaringType || declaringType == type)
            {
                if (cache.Members.ContainsKey(memberInfo.Name))
                    throw new Exception("// TODO VV: deal with this later");

                cache.Members[memberInfo.Name] = member;
            }
            else
            {
                var typeMembers = cache.InheritedMembers.GetOrNew(declaringType);

                if (typeMembers.ContainsKey(memberInfo.Name))
                    throw new Exception("// TODO VV: Deal with this later");

                typeMembers[memberInfo.Name] = member;
            }
        }

        foreach (var propertyInfo in type.GetProperties(bindingFlags))
        {
            if (propertyInfo.GetIndexParameters() is not { Length: > 0 })
                continue;

            cache.Indexers.Add(new ViewVariablesTypeCache.Indexer(propertyInfo));
        }

        // Multidimensional arrays... More like, painful arrays.
        if (type.IsArray && type.GetArrayRank() > 1)
        {
            var getter = type.GetSingleMember("Get") as MethodInfo;
            var setter = type.GetSingleMember("Set") as MethodInfo;

            if (getter != null || setter != null)
            {
                cache.Indexers.Add(new ViewVariablesTypeCache.Indexer(getter, setter));
            }
        }
    }

    private ViewVariablesPath? ResolveByCache(ViewVariablesPath path, string relativePath, Type? declaringType = null)
    {
        var cache = GetCache(path.Type);

        foreach (var handler in GetAllTypeHandlers(cache.Type))
        {
            if (handler.HandlePath(path, relativePath) is {} handledSubpath)
                return handledSubpath;
        }

        var obj = path.Get();
        MemberInfo? info = null;

        if ((declaringType == cache.Type || declaringType == null) && cache.Members.TryGetValue(relativePath, out var member))
        {
            info = member.Info;
        }
        else if (declaringType != null
                 && cache.InheritedMembers.TryGetValue(declaringType, out var inheritedMembers)
                 && inheritedMembers.TryGetValue(relativePath, out var inheritedMember))
        {
            info = inheritedMember.Info;
        }
        else if (declaringType == null)
        {
            foreach (var (_, members) in cache.InheritedMembers)
            {
                foreach (var (name, m) in members)
                {
                    if (name != relativePath)
                        continue;

                    info = m.Info;
                    break;
                }

                if (info != null)
                    break;
            }

            if (info == null)
                return null;
        }
        else
        {
            return null;
        }

        ViewVariablesPath subpath = info switch
        {
            // TODO Deal with this entity manager bullshit like, ????
            FieldInfo or PropertyInfo => new ViewVariablesFieldOrPropertyPath(obj, info, _entMan),
            MethodInfo methodInfo => new ViewVariablesMethodPath(obj, methodInfo),
            _ => throw new InvalidOperationException("Invalid member! Must be a property, field or method.")
        };

        return subpath;
    }

    private ViewVariablesPath? IndexByCache(ViewVariablesPath path, string[] arguments, VVAccess access)
    {
        var cache = GetCache(path.Type);
        var obj = path.Get();

        foreach (var indexer in cache.Indexers)
        {
            Type[] argumentTypes;
            int optionalArguments;

            if (indexer.Info != null)
            {
                var indexParams = indexer.Info.GetIndexParameters();
                argumentTypes = indexParams.Select(p => p.ParameterType).ToArray();
                optionalArguments = indexParams.Count(p => p.IsOptional);
            }
            else
            {
                argumentTypes = indexer.Get?.GetParameters().Select(p => p.ParameterType).ToArray()
                             ?? indexer.Set!.GetParameters()[1..].Select(p => p.ParameterType).ToArray();
                optionalArguments = 0;
            }

            if (DeserializeArguments(argumentTypes, optionalArguments, arguments) is not {} parameters)
                continue;

            object? FakeGet()
            {
                return indexer.Get?.Invoke(obj, parameters);
            }

            void FakeSet(object? value)
            {
                if(access == VVAccess.ReadWrite)
                    indexer.Set?.Invoke(obj, new[] {value}.Concat(parameters).ToArray());
            }

            return indexer.Info is { } info
                ? new ViewVariablesIndexedPath(obj, info, parameters, access)
                : new ViewVariablesFakePath(FakeGet, FakeSet, null,
                    indexer.Get?.ReturnType ?? indexer.Set!.GetParameters()[0].ParameterType);
        }

        return null;
    }

    private IEnumerable<string> ListByCache(ViewVariablesPath path)
    {
        var type = path.Type;
        var cache = GetCache(type);

        foreach (var handler in GetAllTypeHandlers(type))
        {
            foreach (var subpath in handler.ListPath(path))
            {
                yield return subpath;
            }
        }

        foreach (var member in cache.Members.Keys)
        {
            yield return member;
        }

        var obj = path.Get();

        if (obj == null)
            yield break;

        switch (obj)
        {
            // Handle dictionaries and lists specially, for indexing purposes...
            case IDictionary dict:
            {
                var keyType = typeof(void);

                if (type.GenericTypeArguments is {Length: 2} generics)
                {
                    // Assume the key type is the first entry...
                    keyType = generics[0];
                }

                foreach (var key in dict.Keys)
                {
                    string? value;

                    try
                    {
                        var entryType = key.GetType();
                        string? tag = null;

                        // Handle cases such as "Dictionary<object, whatever>"
                        if (entryType != keyType)
                            tag = $"!type:{entryType.Name}";

                        // Forgive me, Paul... We use serv3 to serialize the value into its "text value".
                        value = SerializeValue(entryType, key, tag);
                        if (value == null)
                            continue;

                        // Enclose in parentheses, in case there's a space in the value.
                        if (value.Contains(' '))
                            value = $"({value})";
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    yield return $"[{value}]";
                }

                break;
            }
            case Array array:
            {
                var lowerBounds = Enumerable.Range(0, array.Rank)
                    .Select(i => array.GetLowerBound(i))
                    .ToArray();
                var upperBounds = Enumerable.Range(0, array.Rank)
                    .Select(i => array.GetUpperBound(i))
                    .ToArray();

                var indices = new int[array.Rank];

                lowerBounds.CopyTo(indices, 0);

                while (true)
                {
                    yield return $"[{string.Join(',', indices)}]";

                    var finished = false;

                    for (var i = indices.Length - 1; i >= -1; i--)
                    {
                        // When at -1, this means that we've successfully iterated all dimensions of the array.
                        if (i == -1)
                        {
                            finished = true;
                            break;
                        }

                        indices[i] += 1;

                        if (indices[i] > upperBounds[i])
                        {
                            // We've gone over the upper bound, reset index and increase the next dimension's index.
                            indices[i] = lowerBounds[i];
                            continue;
                        }

                        break;
                    }

                    if (finished)
                        break;
                }

                break;
            }
            // We handle Array specially instead of using IList here because of multi-dimensional arrays and variable-bounds arrays.
            case IList list:
            {
                for (var i = 0; i < list.Count; i++)
                {
                    yield return $"[{i}]";
                }

                break;
            }
            default:
            {
                break;
            }
        }
    }
}

public sealed class ViewVariablesTypeCache
{
    [ViewVariables] public readonly Type Type;
    [ViewVariables] internal readonly ViewVariablesTypeHandler Handler;
    [ViewVariables] internal readonly Dictionary<string, Member> Members = new();
    [ViewVariables] internal readonly Dictionary<Type, Dictionary<string, Member>> InheritedMembers = new();
    [ViewVariables] internal readonly List<Indexer> Indexers = new();

    internal ViewVariablesTypeCache(Type type, ViewVariablesTypeHandler handler)
    {
        Type = type;
        Handler = handler;
    }

    internal readonly struct Member
    {
        [ViewVariables] public readonly MemberInfo Info;

        public Member(MemberInfo info)
        {
            Info = info;
        }
    }

    internal readonly struct Indexer
    {
        [ViewVariables] public readonly PropertyInfo? Info;
        [ViewVariables] public readonly MethodInfo? Get;
        [ViewVariables] public readonly MethodInfo? Set;

        public Indexer(PropertyInfo info)
        {
            Info = info;
            Get = null;
            Set = null;
        }

        public Indexer(MethodInfo? get, MethodInfo? set)
        {
            DebugTools.Assert(get == null && set == null);
            Info = null;
            Get = get;
            Set = set;
        }
    }
}
