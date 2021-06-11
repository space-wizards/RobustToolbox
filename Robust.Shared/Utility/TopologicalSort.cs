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

        [DebuggerDisplay("GraphNode: {" + nameof(System) + "}")]
        public sealed class GraphNode<T>
        {
            public readonly T Value;
            public readonly List<GraphNode<T>> Dependant = new();
            public int DependsOnCount;

            public GraphNode(T value)
            {
                Value = value;
            }
        }
    }
}
