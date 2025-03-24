using System.Runtime.CompilerServices;
using NUnit.Framework;

// So it can use RobustServerSimulation.
[assembly: InternalsVisibleTo("Robust.Benchmarks")]

[assembly: Parallelizable(ParallelScope.Fixtures)]
