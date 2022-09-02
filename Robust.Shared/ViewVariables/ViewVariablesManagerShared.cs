using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.ViewVariables
{
    public delegate (object? obj, string[] segments) DomainResolveObject(string path);

    internal abstract partial class ViewVariablesManagerShared
    {
        [Dependency] private readonly ISerializationManager _serMan = default!;

        private readonly Dictionary<Type, HashSet<object>> _cachedTraits = new();
        private readonly Dictionary<string, DomainData> _registeredDomains = new();

        private const BindingFlags MembersBindings =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Regex IndexerRegex = new (@"\[[^\[]+\]", RegexOptions.Compiled);
        private static readonly Regex TypeSpecifierRegex = new (@"\{[^\{]+\}", RegexOptions.Compiled);

        public virtual void Initialize()
        {
            InitializeDomains();
        }

        public void RegisterDomain(string domain, DomainResolveObject resolveObject)
        {
            _registeredDomains.Add(domain, new DomainData(resolveObject));
        }

        public bool UnregisterDomain(string domain)
        {
            return _registeredDomains.Remove(domain);
        }

        public object? ResolveFullPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            if (path.StartsWith('/'))
                path = path[1..];

            var segments = path.Split('/');

            if (segments.Length == 0)
                return null;

            var domain = segments[0];

            if (!_registeredDomains.TryGetValue(domain, out var domainData))
                return null;

            var (obj, relativePath) = domainData.ResolveObject(string.Join('/', segments[1..]));

            return ResolveRelativePath(obj, relativePath)?.Getter();
        }

        public (Func<object?> Getter, Action<object?> Setter)? ResolveRelativePath(object? obj, string[] segments)
        {
            (Func<object?> Getter, Action<object?> Setter)? indexed = null;
            MemberInfo? memberInfo = null;
            object? parent = null;

            // Who needs recursion, am I right?
            while (true)
            {
                if (obj == null)
                    return null;

                // Empty path, return object itself.
                if (segments.Length == 0)
                {
                    if (memberInfo == null || parent == null)
                        return (() => obj, _ => { });

                    if (indexed != null)
                        return indexed;

                    return (() => memberInfo.GetValue(parent), value => memberInfo.SetValue(parent, value));
                }

                indexed = null;
                var nextSegment = segments[0];

                Type? declaringType = null;

                var specifiers = TypeSpecifierRegex.Matches(nextSegment);
                var indexers = IndexerRegex.Matches(nextSegment);

                // Yeah, that's not valid.
                if (specifiers.Count > 1)
                    return null;

                if (specifiers.Count == 1 && _reflectionMan.GetType(specifiers[0].Value[1..^1]) is { } t)
                {
                    declaringType = t;
                }

                var memberName = TypeSpecifierRegex.Replace(IndexerRegex.Replace(nextSegment, string.Empty), string.Empty);
                memberInfo = obj.GetType().GetSingleMember(memberName, declaringType);

                if (memberInfo == null)
                    return null;

                parent = obj;
                obj = memberInfo.GetValue(obj);

                foreach (Match match in indexers)
                {
                    parent = obj;
                    indexed = ResolveIndexing(obj, ParseArguments(match.Value[1..^1]));
                    obj = indexed?.Getter();
                }

                segments = segments[1..];
            }
        }

        private string[] ParseArguments(string arguments)
        {
            var args = new List<string>();
            var parentheses = false;
            var builder = new StringBuilder();
            var i = 0;

            while (i < arguments.Length)
            {
                var current = arguments[i];
                switch (current)
                {
                    case '(':
                        parentheses = true;
                        break;
                    case ')' when parentheses:
                        parentheses = false;
                        break;
                    case ',' when !parentheses:
                        args.Add(builder.ToString());
                        builder.Clear();
                        break;
                    default:
                        if (!parentheses && char.IsWhiteSpace(current))
                            break;
                        builder.Append(current);
                        break;
                }
                i++;
            }

            args.Add(builder.ToString());
            return args.ToArray();
        }

        private (Func<object?> Getter, Action<object?> Setter)? ResolveIndexing(object? obj, string[] arguments)
        {
            if (obj == null || arguments.Length == 0)
                return null;

            // No indexer.
            if (obj.GetType().GetIndexer() is not {} indexer)
                return null;

            var parametersInfo = indexer.GetIndexParameters();

            // Incorrect number of arguments!
            if (arguments.Length < parametersInfo.Count(p => !p.IsOptional) || arguments.Length > parametersInfo.Length)
                return null;

            var parameters = new List<object?>();

            for (var i = 0; i < arguments.Length; i++)
            {
                var argument = arguments[i];
                var pInfo = parametersInfo[i];

                // Check if the argument is a VV path...
                var value = ResolveFullPath(argument);

                if (value == null)
                {
                    // Here we go serialization moment
                    using TextReader stream = new StringReader(argument);
                    var yamlStream = new YamlStream();
                    yamlStream.Load(stream);
                    var document = yamlStream.Documents[0];
                    var rootNode = document.RootNode;
                    value = _serMan.Read(pInfo.ParameterType, rootNode.ToDataNode());
                }

                parameters.Add(value);
            }

            return (() => indexer.GetValue(obj, parameters.ToArray()), value => indexer.SetValue(obj, value, parameters.ToArray()));
        }

        /// <summary>
        ///     Figures out which VV traits an object type has. This method is in shared so the client and server agree on this mess.
        /// </summary>
        /// <seealso cref="ViewVariablesBlobMetadata.Traits"/>
        public ICollection<object> TraitIdsFor(Type type)
        {
            if (!_cachedTraits.TryGetValue(type, out var traits))
            {
                traits = new HashSet<object>();
                _cachedTraits.Add(type, traits);
                if (ViewVariablesUtility.TypeHasVisibleMembers(type))
                {
                    traits.Add(ViewVariablesTraits.Members);
                }

                if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    traits.Add(ViewVariablesTraits.Enumerable);
                }

                if (typeof(EntityUid).IsAssignableFrom(type))
                {
                    traits.Add(ViewVariablesTraits.Entity);
                }
            }

            return traits;
        }

        internal sealed class DomainData
        {
            public DomainResolveObject ResolveObject;

            public DomainData(DomainResolveObject resolveObject)
            {
                ResolveObject = resolveObject;
            }
        }
    }
}
