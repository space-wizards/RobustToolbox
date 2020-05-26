using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Log;

namespace Robust.Shared.Input.Binding
{
    /// <inheritdoc />
    public class CommandBindRegistry : ICommandBindRegistry
    {
        // all registered bindings
        private List<TypeBinding> _bindings = new List<TypeBinding>();
        // handlers in the order they should be resolved for the given key function.
        // internally we use a graph to construct this but we render it down to a flattened
        // list so we don't need to do any graph traversal at query time
        private readonly Dictionary<BoundKeyFunction, List<InputCmdHandler>> _bindingsForKey;

        /// <inheritdoc />
        public void Register(TypeBindings typeBindings)
        {
            _bindings.AddRange(typeBindings.Bindings);
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
        public void Unregister(Type forType)
        {
            _bindings.RemoveAll(binding => binding.ForType != forType);
            RebuildGraph();
        }

        /// <inheritdoc />
        public void Unregister<T>()
        {
            Unregister(typeof(T));
        }

        private void RebuildGraph()
        {
            // we only need to resolve dependencies between handlers bound to the same key function,
            // so we'll segregate them by key function as the first step
            var functionToBindings = FunctionToBindings();

            // holds our final list of handlers for each key function in the order they should be resolved
            var bindingsForKey = new Dictionary<BoundKeyFunction, List<InputCmdHandler>>();

            foreach (var functionBindings in FunctionToBindings())
            {
                bindingsForKey[functionBindings.Key] = ResolveDependencies(functionBindings.Key, functionBindings.Value);

            }

        }

        private Dictionary<BoundKeyFunction, List<TypeBinding>> FunctionToBindings()
        {
            var functionToBindings = new Dictionary<BoundKeyFunction, List<TypeBinding>>();
            foreach (var typeBinding in _bindings)
            {
                if (!functionToBindings.ContainsKey(typeBinding.BoundKeyFunction))
                {
                    functionToBindings[typeBinding.BoundKeyFunction] = new List<TypeBinding>();
                }

                functionToBindings[typeBinding.BoundKeyFunction].Add(typeBinding);
            }

            return functionToBindings;
        }


        /// <summary>
        /// Determines the order in which the indicated bindings handlers should be resolved for a
        /// particular bound key function
        /// </summary>
        private List<InputCmdHandler> ResolveDependencies(BoundKeyFunction function, List<TypeBinding> bindingsForFunction)
        {
            //TODO: Probably could be optimized if needed! Generally shouldn't be a big issue since there is a relatively
            // tiny amount of bindings

            List<GraphNode> allNodes = new List<GraphNode>();
            Dictionary<Type,List<GraphNode>> typeToNode = new Dictionary<Type, List<GraphNode>>();
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
                foreach (var afterType in curBinding.TypeBinding.After)
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
                foreach (var beforeType in curBinding.TypeBinding.Before)
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
            List<InputCmdHandler> result = new List<InputCmdHandler>();

            foreach (var node in topoSorted)
            {
                result.Add(node.TypeBinding.Handler);
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
            public List<GraphNode> DependsOn = new List<GraphNode>();
            public readonly TypeBinding TypeBinding;

            public GraphNode(TypeBinding typeBinding)
            {
                TypeBinding = typeBinding;
            }
        }


    }
}
