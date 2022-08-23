using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.Input.Binding
{
    /// <inheritdoc cref="ICommandBindRegistry"/>
    public sealed class CommandBindRegistry : ICommandBindRegistry
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

        internal void RebuildGraph()
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
            //TODO: Probably could be optimized if needed! Generally shouldn't be a big issue since there is a relatively
            // tiny amount of bindings

            List<TopologicalSort.GraphNode<TypedCommandBind>> allNodes = new();
            Dictionary<Type,List<TopologicalSort.GraphNode<TypedCommandBind>>> typeToNode = new();
            // build the dict for quick lookup on type
            foreach (var binding in bindingsForFunction)
            {
                if (!typeToNode.ContainsKey(binding.ForType))
                {
                    typeToNode[binding.ForType] = new List<TopologicalSort.GraphNode<TypedCommandBind>>();
                }
                var newNode = new TopologicalSort.GraphNode<TypedCommandBind>(binding);
                typeToNode[binding.ForType].Add(newNode);
                allNodes.Add(newNode);
            }

            //add the graph edges
            foreach (var curBinding in allNodes)
            {
                foreach (var afterType in curBinding.Value.CommandBind.After)
                {
                    // curBinding should always fire after bindings associated with this afterType, i.e.
                    // this binding DEPENDS ON afterTypes' bindings
                    if (typeToNode.TryGetValue(afterType, out var afterBindings))
                    {
                        foreach (var afterBinding in afterBindings)
                        {
                            afterBinding.Dependant.Add(curBinding);
                        }
                    }
                }

                foreach (var beforeType in curBinding.Value.CommandBind.Before)
                {
                    // curBinding should always fire before bindings associated with this beforeType, i.e.
                    // beforeTypes' bindings DEPENDS ON this binding
                    if (typeToNode.TryGetValue(beforeType, out var beforeBindings))
                    {
                        foreach (var beforeBinding in beforeBindings)
                        {
                            curBinding.Dependant.Add(beforeBinding);
                        }
                    }
                }
            }

            //TODO: Log graph structure for debugging

            //use toposort to build the final result
            return TopologicalSort.Sort(allNodes).Select(c => c.CommandBind.Handler).ToList();
        }

        /// <summary>
        /// Command bind which has an associated type.
        /// The only time a client should need to think about the type for a binding is when they are
        /// registering a set of bindings, so we don't include this information in CommandBind
        /// </summary>
        private sealed class TypedCommandBind
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
