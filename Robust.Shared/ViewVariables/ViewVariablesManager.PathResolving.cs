using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Robust.Shared.Reflection;

namespace Robust.Shared.ViewVariables;

internal abstract partial class ViewVariablesManager
{
    private static readonly Regex IndexerRegex = new (@"\[[^\[]+\]", RegexOptions.Compiled);
    private static readonly Regex TypeSpecifierRegex = new (@"\{[^\{]+\}", RegexOptions.Compiled);

    public ViewVariablesPath? ResolvePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        if (path.StartsWith('/'))
            path = path[1..];

        if (path.EndsWith('/'))
            path = path[..^1];

        var segments = path.Split('/');

        // Technically, this should never happen... But hey, better be safe than sorry?
        if (segments.Length == 0)
            return null;

        var domain = segments[0];

        if (!_registeredDomains.TryGetValue(domain, out var domainData))
            return null;

        var (newPath, relativePath) = domainData.ResolveObject(string.Join('/', segments[1..]));

        return ResolveRelativePath(newPath, relativePath);
    }

    private ViewVariablesPath? ResolveRelativePath(ViewVariablesPath? path, string[] segments)
    {
        // Who needs recursion, am I right?
        while (true)
        {
            // Empty path, return current path. This can happen as we slowly take away segments from the array.
            if (segments.Length == 0)
            {
                return path;
            }

            if (path?.Get() is not {} obj)
                return null;

            var nextSegment = segments[0];

            if (string.IsNullOrEmpty(nextSegment))
            {
                // Let's ignore that...
                segments = segments[1..];
                continue;
            }

            var specifiers = TypeSpecifierRegex.Matches(nextSegment);
            var indexers = IndexerRegex.Matches(nextSegment);

            var nextSegmentClean = TypeSpecifierRegex.Replace(
                IndexerRegex.Replace(nextSegment, string.Empty), string.Empty);

            // Yeah, that's not valid bud.
            if (specifiers.Count > 1)
                return null;

            VVAccess? access = null;

            if (specifiers.Count == 1 || ResolveTypeHandlers(path, nextSegmentClean) is not {} customPath)
            {
                Type? declaringType = null;

                if (specifiers.Count == 1 && _reflectionMan.GetType(specifiers[0].Value[1..^1]) is {} t)
                {
                    declaringType = t;
                }

                var memberInfo = obj.GetType().GetSingleMember(nextSegmentClean, declaringType);

                if (memberInfo == null || !ViewVariablesUtility.TryGetViewVariablesAccess(memberInfo, out access))
                    return null;

                path = memberInfo switch
                {
                    FieldInfo or PropertyInfo => new ViewVariablesFieldOrPropertyPath(obj, memberInfo),
                    MethodInfo methodInfo => new ViewVariablesMethodPath(obj, methodInfo),
                    _ => throw new InvalidOperationException("Invalid member! Must be a property, field or method.")
                };
            }
            else
            {
                path = customPath;
                access = VVAccess.ReadWrite;
            }

            // After this, obj is essentially the parent.

            foreach (Match match in indexers)
            {
                path = ResolveIndexing(path, ParseArguments(match.Value[1..^1]), access.Value);
            }

            segments = segments[1..];
        }
    }

    private ViewVariablesPath? ResolveIndexing(ViewVariablesPath? path, string[] arguments, VVAccess access)
    {
        if (path?.Get() is not {} obj || arguments.Length == 0)
            return null;

        var type = obj.GetType();

        // Multidimensional arrays... More like, painful arrays.
        if (type.IsArray && type.GetArrayRank() > 1)
        {
            var getter = type.GetSingleMember("Get") as MethodInfo;
            var setter = type.GetSingleMember("Set") as MethodInfo;

            if (getter == null && setter == null)
                return null;

            var p = DeserializeArguments(
                getter?.GetParameters().Select(p => p.ParameterType).ToArray()
                ?? setter!.GetParameters()[1..].Select(p => p.ParameterType).ToArray(),
                0, arguments);

            object? Get()
            {
                return getter?.Invoke(obj, p);
            }

            void Set(object? value)
            {
                if(p != null && access == VVAccess.ReadWrite)
                    setter?.Invoke(obj, new[] {value}.Concat(p).ToArray());
            }

            return new ViewVariablesFakePath(Get, Set, null, getter?.ReturnType ?? setter!.GetParameters()[0].ParameterType);
        }

        // No indexer.
        if (type.GetIndexer() is not {} indexer)
            return null;

        var parametersInfo = indexer.GetIndexParameters();

        var parameters = DeserializeArguments(
            parametersInfo.Select(p => p.ParameterType).ToArray(),
            parametersInfo.Count(p => p.IsOptional),
            arguments);

        if (parameters == null)
            return null;

        return new ViewVariablesIndexedPath(obj, indexer, parameters, access);
    }

    private ViewVariablesPath? ResolveTypeHandlers(ViewVariablesPath path, string relativePath)
    {
        if (path.Get() is not {} obj
            || string.IsNullOrEmpty(relativePath)
            || relativePath.Contains('/'))
            return null;

        foreach (var handler in GetAllTypeHandlers(obj.GetType()))
        {
            if (handler.HandlePath(path, relativePath) is {} newPath)
                return newPath;
        }

        // Not handled by a custom type handler!
        return null;
    }
}
