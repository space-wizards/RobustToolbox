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
        ViewVariablesComponentPath? oldComp;

        // Who needs recursion, am I right?
        while (true)
        {
            if (path is ViewVariablesComponentPath compPath)
                path.ParentComponent = compPath;

            oldComp = path?.ParentComponent;

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

            Type? declaringType = null;

            if (specifiers.Count == 1 && _reflectionMan.GetType(specifiers[0].Value[1..^1]) is {} t)
            {
                declaringType = t;
            }

            // TODO: Set access properly!
            VVAccess? access = VVAccess.ReadWrite;

            path = ResolveByCache(path, nextSegmentClean, declaringType);
            UpdateParentComp(path, ref oldComp);

            if (path == null)
                return null;

            // After this, obj is essentially the parent.
            foreach (Match match in indexers)
            {
                path = IndexByCache(path!, ParseArguments(match.Value[1..^1]), access.Value);
                UpdateParentComp(path, ref oldComp);
            }

            segments = segments[1..];
        }
    }

    private void UpdateParentComp(ViewVariablesPath? newPath, ref ViewVariablesComponentPath? oldPath)
    {
        if (newPath == null)
            return;

        if (newPath is ViewVariablesComponentPath newCompPath)
            newPath.ParentComponent = newCompPath;
        else if (newPath != null)
            newPath.ParentComponent ??= oldPath;

        oldPath = newPath?.ParentComponent;
    }
}
