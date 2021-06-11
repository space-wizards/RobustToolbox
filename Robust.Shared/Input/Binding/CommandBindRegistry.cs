using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.Input.Binding
{
    /// <inheritdoc cref="ICommandBindRegistry"/>
    public class CommandBindRegistry : ICommandBindRegistry
    {
        // all registered bindings
        private List<TypedCommandBind> _bindings = new();
        // handlers in the order they should be resolved for the given key function.
        // internally we use a graph to construct this but we render it down to a flattened
        // list so we don't need to do any graph traversal at query time
        private Dictionary<BoundKeyFunction, List<InputCmdHandler>> _bindingsForKey = new();
        private bool _graphDirty = false;

        /// <inheritdoc />
        public void Register<TOwner>(CommandBinds commandBinds)
        {
            Register(commandBinds, typeof(TOwner));
        }

        /// <inheritdoc />
        public void Register(CommandBinds commandBinds, Type owner)
        {
            if (_bindings.Any(existing => existing.ForType == owner))
            {
                // feel free to delete this if there's an actual need for registering multiple
                // bindings for a given type in separate calls to Register()
                Logger.Warning("Command binds already registered for type {0}, but you are trying" +
                               " to register more. This may " +
                               "be a programming error. Did you register these under the wrong type, or " +
                               "did you forget to unregister these bindings when" +
                               " your system / manager is shutdown?", owner.Name);
            }

            foreach (var binding in commandBinds.Bindings)
            {
                _bindings.Add(new TypedCommandBind(owner, binding));
            }

            _graphDirty = true;
        }

        /// <inheritdoc />
        public IEnumerable<InputCmdHandler> GetHandlers(BoundKeyFunction function)
        {
            if (_graphDirty)
                RebuildGraph();

            if (_bindingsForKey.TryGetValue(function, out var handlers))
            {
                return handlers;
            }
            return Enumerable.Empty<InputCmdHandler>();
        }

        /// <inheritdoc />
        public void Unregister(Type owner)
        {
            _bindings.RemoveAll(binding => binding.ForType == owner);

            _graphDirty = true;
        }

        /// <inheritdoc />
        public void Unregister<TOwner>()
        {
            Unregister(typeof(TOwner));
        }

        private void RebuildGraph()
        {
            _bindingsForKey.Clear();

            foreach (var functionBindings in FunctionToBindings())
            {
                _bindingsForKey[functionBindings.Key] = ResolveDependencies(functionBindings.Key, functionBindings.Value);

            }

            _graphDirty = false;
        }

        private Dictionary<BoundKeyFunction, List<TypedCommandBind>> FunctionToBindings()
        {
            var functionToBindings = new Dictionary<BoundKeyFunction, List<TypedCommandBind>>();
            foreach (var typeBinding in _bindings)
            {
                if (!functionToBindings.ContainsKey(typeBinding.CommandBind.BoundKeyFunction))
                {
                    functionToBindings[typeBinding.CommandBind.BoundKeyFunction] = new List<TypedCommandBind>();
                }

                functionToBindings[typeBinding.CommandBind.BoundKeyFunction].Add(typeBinding);
            }

            return functionToBindings;
        }


        /// <summary>
        /// Determines the order in which the indicated bindings handlers should be resolved for a
        /// particular bound key function
        /// </summary>
        private List<InputCmdHandler> ResolveDependencies(BoundKeyFunction function, List<TypedCommandBind> bindingsForFunction)
        {
            //TODO: Log graph structure for debugging

            var dict = bindingsForFunction.ToDictionary(b => b.ForType, b => b.CommandBind);

            var nodes = TopologicalSort.FromBeforeAfter(
                bindingsForFunction,
                bind => bind.ForType,
                bind => bind.CommandBind.Before,
                bind => bind.CommandBind.After);

            var topoSorted = TopologicalSort.Sort(nodes);

            return topoSorted.Select(node => dict[node].Handler).ToList();
        }

        /// <summary>
        /// node in our temporary dependency graph
        /// </summary>
        private class GraphNode
        {
            public List<GraphNode> DependsOn = new();
            public readonly TypedCommandBind TypedCommandBind;

            public GraphNode(TypedCommandBind typedCommandBind)
            {
                TypedCommandBind = typedCommandBind;
            }
        }

        /// <summary>
        /// Command bind which has an associated type.
        /// The only time a client should need to think about the type for a binding is when they are
        /// registering a set of bindings, so we don't include this information in CommandBind
        /// </summary>
        private class TypedCommandBind
        {
            public readonly Type ForType;
            public readonly CommandBind CommandBind;

            public TypedCommandBind(Type forType, CommandBind commandBind)
            {
                ForType = forType;
                CommandBind = commandBind;
            }
        }


    }
}
