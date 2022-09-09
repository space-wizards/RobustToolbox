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
    public delegate IEnumerable<string>? DomainListPaths(string[] segments);
    public delegate ViewVariablesPath? HandleTypePath(object? obj, string relativePath);
    public delegate ViewVariablesPath? HandleTypePath<in T>(T? obj, string relativePath);
    public delegate IEnumerable<string> ListTypeCustomPaths(object? obj);
    public delegate IEnumerable<string> ListTypeCustomPaths<in T>(T? obj);

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

            IEnumerable<string> ListHandler(object? obj)
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

            // Technically, this should never happen... But hey, better be safe than sorry?
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

            if (_registeredTypeHandlers.TryGetValue(type, out var typeData))
            {
                paths.AddRange(typeData.List(obj));
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

                ListIndexers(memberObj, name, paths);
            }

            ListIndexers(obj, string.Empty, paths);

            return Full(path, paths);
        }

        private void ListIndexers(object? obj, string name, List<string> paths)
        {
            if (obj == null)
                return;

            // Handle dictionaries and lists specially, for indexing purposes...
            if (obj is IDictionary dict)
            {
                foreach (var key in dict.Keys)
                {
                    try
                    {
                        // Forgive me, Paul... We use serv3 to serialize the value into its "text value".
                        var value = _serMan.WriteValue(key.GetType(), key).ToYamlNode().ToString();
                        paths.Add($"{name}[{value}]");
                    }
                    catch (Exception)
                    {
                        // Nada.
                    }
                }
            }
            else if (obj is Array array)
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
            }
            // We handle Array specially instead of using IList here because of multi-dimensional arrays and variable-bounds arrays.
            else if (obj is IList list)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    paths.Add($"{name}[{i}]");
                }
            }
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
                    if(p != null)
                        setter?.Invoke(obj, new[] {value}.Concat(p).ToArray());
                }

                return new ViewVariablesFakePath(Get, Set, null, getter?.ReturnType ?? setter!.GetParameters()[0].ParameterType);
            }

            // No indexer.
            if (type.GetIndexer() is not { } indexer)
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

            try
            {
                // Here we go serialization moment
                using TextReader stream = new StringReader(value);
                var yamlStream = new YamlStream();
                yamlStream.Load(stream);
                var document = yamlStream.Documents[0];
                var rootNode = document.RootNode;
                return _serMan.Read(type, rootNode.ToDataNode());
            }
            catch (Exception)
            {
                return null;
            }
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
