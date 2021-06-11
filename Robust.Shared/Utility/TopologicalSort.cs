using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Robust.Shared.Utility
{
    public sealed class TopologicalSort
    {
        public static IEnumerable<T> Sort<T>(IEnumerable<GraphNode<T>> nodes)
        {
            var totalVerts = 0;
            var empty = new Queue<GraphNode<T>>();

            var nodesArray = nodes.ToArray();

            foreach (var node in nodesArray)
            {
                totalVerts += 1;

                foreach (var dep in node.Dependant)
                {
                    dep.DependsOnCount += 1;
                }
            }

            foreach (var node in nodesArray)
            {
                if (node.DependsOnCount == 0)
                    empty.Enqueue(node);
            }

            while (empty.TryDequeue(out var node))
            {
                yield return node.Value;
                totalVerts -= 1;

                foreach (var dep in node.Dependant)
                {
                    dep.DependsOnCount -= 1;
                    if (dep.DependsOnCount == 0)
                        empty.Enqueue(dep);
                }
            }

            if (totalVerts != 0)
                throw new InvalidOperationException("Graph contained cycle(s).");
        }

        // I will never stop using the word "datum".
        public static IEnumerable<GraphNode<TValue>> FromBeforeAfter<TDatum, TValue>(
            IEnumerable<TDatum> data,
            Func<TDatum, TValue> keySelector,
            Func<TDatum, IEnumerable<TValue>> beforeSelector,
            Func<TDatum, IEnumerable<TValue>> afterSelector,
            bool allowMissing=false)
            where TValue : notnull
        {
            var dict = new Dictionary<TValue, (TDatum datum, GraphNode<TValue> node)>();

            foreach (var datum in data)
            {
                var key = keySelector(datum);
                dict.Add(key, (datum, new GraphNode<TValue>(key)));
            }

            foreach (var (key, (datum, node)) in dict)
            {
                foreach (var before in beforeSelector(datum))
                {
                    if (dict.TryGetValue(before, out var entry))
                    {
                        node.Dependant.Add(entry.node);
                    }
                    else if (!allowMissing)
                    {
                        throw new InvalidOperationException($"Vertex '{before}' referenced by '{key}' was not found in the graph.");
                    }
                }

                foreach (var after in afterSelector(datum))
                {
                    if (dict.TryGetValue(after, out var entry))
                    {
                        entry.node.Dependant.Add(node);
                    }
                    else if (!allowMissing)
                    {
                        throw new InvalidOperationException($"Vertex '{after}' referenced by '{key}' was not found in the graph.");
                    }
                }
            }

            return dict.Values.Select(c => c.node);
        }

        [DebuggerDisplay("GraphNode: {" + nameof(Value) + "}")]
        public class GraphNode<T>
        {
            public readonly T Value;
            public readonly List<GraphNode<T>> Dependant = new();

            // Used internal by sort implementation, do not touch.
            internal int DependsOnCount;

            public GraphNode(T value)
            {
                Value = value;
            }
        }
    }
}
