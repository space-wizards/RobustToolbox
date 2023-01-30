using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

namespace Robust.Shared.ViewVariables;

internal abstract partial class ViewVariablesManager
{
    private static readonly Regex IndexerRegex = new (@"\[[^\]]+\]", RegexOptions.Compiled);
    private static readonly Regex TypeSpecifierRegex = new (@"\{[^\}]+\}", RegexOptions.Compiled);

    public bool TryResolvePath(string path, [NotNullWhen(true)] out ViewVariablesPath? result, out string? error)
    {
        error = null;
        result = null;
        if (string.IsNullOrEmpty(path))
            return false;

        if (path.StartsWith('/'))
            path = path[1..];

        if (path.EndsWith('/'))
            path = path[..^1];

        var segments = path.Split('/');

        // Technically, this should never happen... But hey, better be safe than sorry?
        if (segments.Length == 0)
            return false;

        var domain = segments[0];

        if (!_registeredDomains.TryGetValue(domain, out var domainData))
        {
            error = "Invalid domain";
            return false;
        }

        var (newPath, relativePath) = domainData.ResolveObject(string.Join('/', segments[1..]));

        return TryResolveRelativePath(newPath, relativePath, out result, out error);
    }

    private bool TryResolveRelativePath(ViewVariablesPath? path, string[] segments, [NotNullWhen(true)] out ViewVariablesPath? result, out string? error)
    {
        result = null;
        error = null;

        // Who needs recursion, am I right?
        while (true)
        {
            // Empty path, return current path. This can happen as we slowly take away segments from the array.
            if (segments.Length == 0)
            {
                result = path;
                return result != null;
            }

            if (path?.Get() is not {} obj)
                return false;

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
                return false;

            VVAccess? access;

            if (specifiers.Count == 1 || ResolveTypeHandlers(path, nextSegmentClean) is not {} customPath)
            {
                Type? type = obj.GetType();

                if (specifiers.Count == 1 && _reflectionMan.LooseGetType(specifiers[0].Value[1..^1]) is {} specifiedType)
                {
                    if (!specifiedType.IsSubclassOf(type))
                        throw new InvalidOperationException($"Invalid type specifier. {specifiedType.Name} is not a subclass of {type.Name}");
                    type = specifiedType;
                }

                var memberInfo = type.GetSingleMemberRecursive(nextSegmentClean, flags: MembersBindings);

                if (memberInfo == null)
                {
                    error = $"Non-existent member {nextSegmentClean}";
                    return false;
                }

                if (!ViewVariablesUtility.TryGetViewVariablesAccess(memberInfo, out access))
                {
                    error = $"Member {nextSegmentClean} does not have vv attribute.";
                    return false;
                }

                switch (memberInfo)
                {
                    case FieldInfo:
                    case PropertyInfo:
                        path = new ViewVariablesFieldOrPropertyPath(obj, memberInfo);
                        break;
                    case MethodInfo methodInfo:
                        path = new ViewVariablesMethodPath(obj, methodInfo);
                        break;
                    default:
                        error = "Invalid member! Must be a property, field or method.";
                        return false;
                }
            }
            else
            {
                path = customPath;
                access = VVAccess.ReadWrite;
            }

            // After this, obj is essentially the parent.

            foreach (Match match in indexers)
            {
                if (!TryResolveIndexing(path, ParseArguments(match.Value[1..^1]), access.Value, out var indexed, out error))
                    return false;

                path = indexed;
            }

            segments = segments[1..];
        }
    }

    private bool TryResolveIndexing(ViewVariablesPath? path, string[] arguments, VVAccess access, [NotNullWhen(true)] out ViewVariablesPath? result, out string? error)
    {
        error = null;
        result = null;
        if (path?.Get() is not {} obj || arguments.Length == 0)
            return false;

        var type = obj.GetType();

        // Multidimensional arrays... More like, painful arrays.
        if (type.IsArray && type.GetArrayRank() > 1)
        {
            var getter = type.GetSingleMember("Get", flags: MembersBindings) as MethodInfo;
            var setter = type.GetSingleMember("Set", flags: MembersBindings) as MethodInfo;

            if (getter == null && setter == null)
            {
                error = "Cannot index object without getter or setter";
                result = null;
                return false;
            }

            if (!TryDeserializeArguments(
                getter?.GetParameters().Select(p => p.ParameterType).ToArray()
                ?? setter!.GetParameters()[1..].Select(p => p.ParameterType).ToArray(),
                0, arguments, out var p, out error))
            {
                return false;
            }

            object? Get()
            {
                return getter?.Invoke(obj, p);
            }

            void Set(object? value)
            {
                if(p != null && access == VVAccess.ReadWrite)
                    setter?.Invoke(obj, new[] {value}.Concat(p).ToArray());
            }

            result = new ViewVariablesFakePath(Get, Set, null, getter?.ReturnType ?? setter!.GetParameters()[0].ParameterType);
            return true;
        }

        // No indexer.
        if (type.GetIndexer(MembersBindings) is not {} indexer)
        {
            error = "Object has no indexer";
            return false;
        }

        var parametersInfo = indexer.GetIndexParameters();

        if (!TryDeserializeArguments(
            parametersInfo.Select(p => p.ParameterType).ToArray(),
            parametersInfo.Count(p => p.IsOptional),
            arguments, out var parameters, out error))
        {
            return false;
        }

        result = new ViewVariablesIndexedPath(obj, indexer, parameters, access);
        return true;
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
