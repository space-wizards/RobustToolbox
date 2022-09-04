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
    public delegate (ViewVariablesPath? path, string[] segments) DomainResolveObject(string path);
    public delegate string[] DomainListPaths(string[] segments);
    public delegate ViewVariablesPath? HandleTypePath(object? obj, string relativePath);
    public delegate ViewVariablesPath? HandleTypePath<in T>(T? obj, string relativePath);
    public delegate string[] ListTypeCustomPaths(object? obj);
    public delegate string[] ListTypeCustomPaths<in T>(T? obj);

    internal abstract partial class ViewVariablesManager : IViewVariablesManager
    {
        [Dependency] private readonly ISerializationManager _serMan = default!;

        private readonly Dictionary<Type, HashSet<object>> _cachedTraits = new();
        private readonly Dictionary<string, DomainData> _registeredDomains = new();
        private readonly Dictionary<Type,  TypeHandlerData> _registeredTypeHandlers = new();

        private const BindingFlags MembersBindings =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Regex IndexerRegex = new (@"\[[^\[]+\]", RegexOptions.Compiled);
        private static readonly Regex TypeSpecifierRegex = new (@"\{[^\{]+\}", RegexOptions.Compiled);

        public virtual void Initialize()
        {
            InitializeDomains();
            InitializeTypeHandlers();
        }

        public void RegisterDomain(string domain, DomainResolveObject resolveObject, DomainListPaths list)
        {
            _registeredDomains.Add(domain, new DomainData(resolveObject, list));
        }

        public bool UnregisterDomain(string domain)
        {
            return _registeredDomains.Remove(domain);
        }

        public void RegisterTypeHandler<T>(HandleTypePath<T> handler, ListTypeCustomPaths<T> list)
        {
            ViewVariablesPath? Handler(object? obj, string relPath)
                => handler((T?) obj, relPath);

            string[] ListHandler(object? obj)
                => list((T?) obj);

            RegisterTypeHandler(typeof(T), Handler, ListHandler);
        }

        public void RegisterTypeHandler(Type type, HandleTypePath handler, ListTypeCustomPaths list)
        {
            if (_registeredTypeHandlers.ContainsKey(type))
                throw new Exception("Duplicated registration!");

            _registeredTypeHandlers[type] = new TypeHandlerData(handler, list);
        }

        public bool UnregisterTypeHandler<T>()
        {
            return UnregisterTypeHandler(typeof(T));
        }

        public bool UnregisterTypeHandler(Type type)
        {
            return _registeredTypeHandlers.Remove(type);
        }

        public ViewVariablesPath? ResolvePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            if (path.StartsWith('/'))
                path = path[1..];

            if (path.EndsWith('/'))
                path = path[..^1];

            var segments = path.Split('/');

            if (segments.Length == 0)
                return null;

            var domain = segments[0];

            if (!_registeredDomains.TryGetValue(domain, out var domainData))
                return null;

            var (newPath, relativePath) = domainData.ResolveObject(string.Join('/', segments[1..]));

            return ResolveRelativePath(newPath, relativePath);
        }

        public object? ReadPath(string path)
        {
            return ResolvePath(path)?.Get();
        }

        public void WritePath(string path, string value)
        {
            var resPath = ResolvePath(path);
            resPath?.Set(DeserializeValue(resPath.Type, value));
        }

        public object? InvokePath(string path, string arguments)
        {
            var resPath = ResolvePath(path);

            if (resPath == null)
                return null;

            var args = ParseArguments(arguments);

            var desArgs =
                DeserializeArguments(resPath.InvokeParameterTypes, (int)resPath.InvokeOptionalParameters, args);

            return resPath.Invoke(desArgs);
        }

        public string[] ListPath(string path, VVAccess minimumAccess = VVAccess.ReadOnly)
        {
            string[] Domains()
                => _registeredDomains.Keys.Select(d => $"/{d}")
                    .ToArray();

            string[] Full(string fullPath, string[] relativePaths)
                => relativePaths
                    .Select(p =>
                    {
                        if (!fullPath.StartsWith('/'))
                            fullPath = $"/{fullPath}";
                        if (fullPath.EndsWith('/'))
                            fullPath = fullPath[..^1];
                        return string.Join('/', fullPath, p);
                    })
                    .ToArray();

            if (path.StartsWith('/'))
                path = path[1..];

            if (string.IsNullOrEmpty(path))
                return Domains();

            var segments = path.Split('/');

            if (segments.Length == 0)
                return Domains();

            var domain = segments[0];

            if (!_registeredDomains.TryGetValue(domain, out var data))
                return Domains();

            var domainList = data.List(segments[1..]);

            if (domainList.Length != 0)
                return Full($"/{domain}", domainList);

            // Expensive :(
            var resolved = ResolvePath(path);

            if (resolved?.Get() is not {} obj)
            {
                // Okay maybe the last segment is not full? Resolve the prior path
                segments = segments[..^1];
                path = string.Join('/', segments);
                resolved = ResolvePath(path);

                if(resolved?.Get() is not {} priorObj)
                    return Array.Empty<string>();

                obj = priorObj;
            }

            var paths = new List<string>();

            var type = obj.GetType();

            if (_registeredTypeHandlers.TryGetValue(type, out var typeData))
            {
                paths.AddRange(typeData.List(obj));
            }

            // Starts with all custom paths, so the user knows whether to use a specifier or not for hidden native members.
            var uniqueMemberNames = new HashSet<string>(paths);

            foreach (var memberInfo in type.GetMembers(MembersBindings).OrderBy(m => m.DeclaringType == type))
            {
                if (!ViewVariablesUtility.TryGetViewVariablesAccess(memberInfo, out var memberAccess))
                    continue;

                if (memberAccess < minimumAccess)
                    continue;

                var name = memberInfo.Name;

                if (!uniqueMemberNames.Add(name))
                    name = @$"{name}{{{memberInfo.DeclaringType?.FullName ?? typeof(void).FullName}}}";

                paths.Add(name);
            }

            return Full(path, paths.ToArray());
        }

        private ViewVariablesPath? ResolveRelativePath(ViewVariablesPath? path, string[] segments)
        {
            // Who needs recursion, am I right?
            while (true)
            {
                // Empty path, return current path.
                if (segments.Length == 0)
                {
                    return path;
                }

                if (path?.Get() is not {} obj)
                    return null;

                var nextSegment = segments[0];

                var specifiers = TypeSpecifierRegex.Matches(nextSegment);
                var indexers = IndexerRegex.Matches(nextSegment);

                var nextSegmentClean = TypeSpecifierRegex.Replace(
                    IndexerRegex.Replace(nextSegment, string.Empty), string.Empty);

                // Yeah, that's not valid bud.
                if (specifiers.Count > 1)
                    return null;

                VVAccess? access = null;

                if (specifiers.Count == 1 || ResolveTypeHandlers(obj, nextSegmentClean) is not {} customPath)
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

            if(builder.Length != 0)
                args.Add(builder.ToString());

            return args.ToArray();
        }

        private ViewVariablesPath? ResolveIndexing(ViewVariablesPath? path, string[] arguments, VVAccess access)
        {
            if (path?.Get() is not {} obj || arguments.Length == 0)
                return null;

            // No indexer.
            if (obj.GetType().GetIndexer() is not {} indexer)
                return null;

            var parametersInfo = indexer.GetIndexParameters();

            var parameters = DeserializeArguments(
                parametersInfo.Select(p => p.ParameterType).ToArray(),
                parametersInfo.Count(p => p.IsOptional),
                arguments);

            return new ViewVariablesIndexedPath(obj, indexer, parameters, access);
        }

        private object?[]? DeserializeArguments(Type[] argumentTypes, int optionalArguments, string[] arguments)
        {
            // Incorrect number of arguments!
            if (arguments.Length < argumentTypes.Length - optionalArguments || arguments.Length > argumentTypes.Length)
                return null;

            var parameters = new List<object?>();

            for (var i = 0; i < arguments.Length; i++)
            {
                var argument = arguments[i];
                var type = argumentTypes[i];

                var value =  DeserializeValue(type, argument);

                parameters.Add(value);
            }

            for (var i = 0; i < argumentTypes.Length - arguments.Length; i++)
            {
                parameters.Add(Type.Missing);
            }

            return parameters.ToArray();
        }

        private object? DeserializeValue(Type type, string value)
        {
            // Check if the argument is a VV path, and if not, deserialize the value with serv3.
            if (ResolvePath(value)?.Get() is {} resolved && resolved.GetType().IsAssignableTo(type))
                return resolved;

            // Here we go serialization moment
            using TextReader stream = new StringReader(value);
            var yamlStream = new YamlStream();
            yamlStream.Load(stream);
            var document = yamlStream.Documents[0];
            var rootNode = document.RootNode;
            return _serMan.Read(type, rootNode.ToDataNode());
        }

        private ViewVariablesPath? ResolveTypeHandlers(object? obj, string relativePath)
        {
            if (obj == null
                || string.IsNullOrEmpty(relativePath)
                || relativePath.Contains('/'))
                return null;

            var type = obj.GetType();

            // First go through the inheritance chain, from current type to base types...
            while (type != null)
            {
                if (_registeredTypeHandlers.TryGetValue(type, out var data))
                {

                    var path = data.Handle(obj, relativePath);

                    if (path != null)
                        return path;
                }

                type = type.BaseType;
            }

            // Then go through all the implemented interfaces, if any.
            foreach (var interfaceType in obj.GetType().GetInterfaces())
            {
                if (_registeredTypeHandlers.TryGetValue(interfaceType, out var data))
                {
                    var path = data.Handle(obj, relativePath);

                    if (path != null)
                        return path;
                }
            }

            return null;
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
            public readonly DomainResolveObject ResolveObject;
            public readonly DomainListPaths List;

            public DomainData(DomainResolveObject resolveObject, DomainListPaths list)
            {
                ResolveObject = resolveObject;
                List = list;
            }
        }

        internal sealed class TypeHandlerData
        {
            public readonly HandleTypePath Handle;
            public readonly ListTypeCustomPaths List;

            public TypeHandlerData(HandleTypePath handle, ListTypeCustomPaths list)
            {
                Handle = handle;
                List = list;
            }
        }
    }
}
