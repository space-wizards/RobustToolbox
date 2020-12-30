using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Log;

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
        private Dictionary<BoundKeyFunction, List<InputCmdHandler>> _bindingsForKey =
            new();

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

            RebuildGraph();
        }

        /// <inheritdoc />
        public IEnumerable<InputCmdHandler> GetHandlers(BoundKeyFunction function)
        {
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
            RebuildGraph();
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

            List<GraphNode> allNodes = new();
            Dictionary<Type,List<GraphNode>> typeToNode = new();
            // build the dict for quick lookup on type
            foreach (var binding in bindingsForFunction)
            {
                if (!typeToNode.ContainsKey(binding.ForType))
                {
                    typeToNode[binding.ForType] = new List<GraphNode>();
                }
                var newNode = new GraphNode(binding);
                typeToNode[binding.ForType].Add(newNode);
                allNodes.Add(newNode);
            }

            //add the graph edges
            foreach (var curBinding in allNodes)
            {
                foreach (var afterType in curBinding.TypedCommandBind.CommandBind.After)
                {
                    // curBinding should always fire after bindings associated with this afterType, i.e.
                    // this binding DEPENDS ON afterTypes' bindings
                    if (typeToNode.TryGetValue(afterType, out var afterBindings))
                    {
                        foreach (var afterBinding in afterBindings)
                        {
                            curBinding.DependsOn.Add(afterBinding);
                        }
                    }
                }
                foreach (var beforeType in curBinding.TypedCommandBind.CommandBind.Before)
                {
                    // curBinding should always fire before bindings associated with this beforeType, i.e.
                    // beforeTypes' bindings DEPENDS ON this binding
                    if (typeToNode.TryGetValue(beforeType, out var beforeBindings))
                    {
                        foreach (var beforeBinding in beforeBindings)
                        {
                            beforeBinding.DependsOn.Add(curBinding);
                        }
                    }
                }
            }

            //TODO: Log graph structure for debugging

            //use toposort to build the final result
            var topoSorted = TopologicalSort(allNodes, function);
            List<InputCmdHandler> result = new();

            foreach (var node in topoSorted)
            {
                result.Add(node.TypedCommandBind.CommandBind.Handler);
            }

            return result;
        }

        //Adapted from https://stackoverflow.com/a/24058279
        private static IEnumerable<GraphNode> TopologicalSort(IEnumerable<GraphNode> nodes, BoundKeyFunction function)
        {
            var elems = nodes.ToDictionary(node => node,
                node => new HashSet<GraphNode>(node.DependsOn));
            while (elems.Count > 0)
            {
                var elem =
                    elems.FirstOrDefault(x => x.Value.Count == 0);
                if (elem.Key == null)
                {
                    throw new InvalidOperationException("Found circular dependency when resolving" +
                                                        $" command binding handler order for key function {function.FunctionName}." +
                                                        $" Please check the systems which register bindings for" +
                                                        $" this function and eliminate the circular dependency.");
                }
                elems.Remove(elem.Key);
                foreach (var selem in elems)
                {
                    selem.Value.Remove(elem.Key);
                }
                yield return elem.Key;
            }
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
