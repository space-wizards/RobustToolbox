using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Reflection;

namespace Robust.Shared.ViewVariables;

internal abstract partial class ViewVariablesManager
{
    public IEnumerable<string> ListPath(string path, VVListPathOptions options)
    {
        // Helper to return all domains' full paths.
        IEnumerable<string> Domains()
            => _registeredDomains.Keys.Select(d => $"/{d}");

        // Helper to return full paths when given a full path and a number of paths relative to it.
        IEnumerable<string> Full(string fullPath, IEnumerable<string> relativePaths)
        {
            if (!fullPath.StartsWith('/'))
                fullPath = $"/{fullPath}";
            if (fullPath.EndsWith('/'))
                fullPath = fullPath[..^1];

            return relativePaths
                .Select(p
                    => p.StartsWith("[")
                        ? $"{fullPath}{p}" // Indexers for the path
                        : string.Join('/', fullPath, p)); // Actual relative paths
        }

        if (path.StartsWith('/'))
            path = path[1..];

        if (string.IsNullOrEmpty(path))
            return Domains();

        var segments = path.Split('/');

        if (segments.Length == 0)
            return Domains();

        var domain = segments[0];

        // If the specified domain of the path does not exist, return a list of paths instead.
        if (!_registeredDomains.TryGetValue(domain, out var data))
            return Domains();

        // Let the domain handle listing its paths...
        var domainList = data.List(segments[1..]);

        // If the domain returned null, that means we're dealing with a path we need to resolve.
        if (domainList != null)
            return Full($"/{domain}", domainList);

        // Expensive :'(
        var resolved = ResolvePath(path);

        // Attempt to get an object from the path...
        if (resolved?.Get() is not {} obj)
        {
            // Okay maybe the last segment of the path is not full? Attempt to resolve the prior path
            segments = segments[..^1];
            path = string.Join('/', segments);
            resolved = ResolvePath(path);

            // If not even that worked, we probably just have an invalid path here... Return nothing.
            if(resolved?.Get() is not {} priorObj)
                return Enumerable.Empty<string>();

            obj = priorObj;
        }

        // We need a place to store all the relative paths we find! TODO: Perhaps just yield instead?
        var paths = new List<string>();

        var type = obj.GetType();

        // List paths from type handler, taking into account base types.
        foreach (var typeData in GetAllTypeHandlers(type))
        {
            paths.AddRange(typeData.ListPath(resolved));
        }

        // We also use a set here for unique names as we need to handle member hiding properly...
        // Starts with all custom paths, as those can hide the "native" members of the object.
        var uniqueMemberNames = new HashSet<string>(paths);

        // For member hiding handling purposes, we handle the members declared by the object's type itself first.
        foreach (var memberInfo in type.GetMembers(MembersBindings).OrderBy(m => m.DeclaringType == type))
        {
            // Ignore the member if it's not a VV member.
            if (!ViewVariablesUtility.TryGetViewVariablesAccess(memberInfo, out var memberAccess))
                continue;

            // Also take access level into account.
            if (memberAccess < options.MinimumAccess)
                continue;

            var name = memberInfo.Name;

            // If the member name is not unique, we adds the type specifier to it.
            if (!uniqueMemberNames.Add(name))
                name = @$"{name}{{{memberInfo.DeclaringType?.FullName ?? typeof(void).FullName}}}";

            paths.Add(name);

            var memberObj = memberInfo.GetValue(obj);

            if(options.ListIndexers)
                ListIndexers(memberObj, name, paths);
        }

        if(options.ListIndexers)
            ListIndexers(obj, string.Empty, paths);

        return Full(path, paths);
    }

    private void ListIndexers(object? obj, string name, List<string> paths)
    {
        switch (obj)
        {
            // Handle dictionaries and lists specially, for indexing purposes...
            case IDictionary dict:
            {
                var keyType = typeof(void);

                if (dict.GetType().GenericTypeArguments is {Length: 2} generics)
                {
                    // Assume the key type is the first entry...
                    keyType = generics[0];
                }

                foreach (var key in dict.Keys)
                {
                    try
                    {
                        var type = key.GetType();
                        string? tag = null;

                        // Handle cases such as "Dictionary<object, whatever>"
                        if (type != keyType)
                            tag = $"!type:{type.Name}";

                        // Forgive me, Paul... We use serv3 to serialize the value into its "text value".
                        if (SerializeValue(type, key, tag) is not {} value)
                            continue;

                        // Enclose in parentheses, in case there's a space in the value.
                        if (value.Contains(' '))
                            value = $"({value})";

                        paths.Add($"{name}[{value}]");
                    }
                    catch (Exception)
                    {
                        // Nada.
                    }
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
                    paths.Add($"{name}[{string.Join(',', indices)}]");

                    var finished = false;

                    for (var i = indices.Length - 1; i >= -1; i--)
                    {
                        // When at -1, this means that we've successfully iterated all dimensions of the array.
                        if (i == -1)
                        {
                            finished = true;
                            break;
                        }

                        ref var index = ref indices[i];
                        index += 1;

                        if (index > upperBounds[i])
                        {
                            // We've gone over the upper bound, reset index and increase the next dimension's index.
                            index = lowerBounds[i];
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
                    paths.Add($"{name}[{i}]");
                }

                break;
            }
            default:
            {
                return;
            }
        }
    }
}
