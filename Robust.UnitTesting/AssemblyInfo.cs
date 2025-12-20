using System.Runtime.CompilerServices;
using NUnit.Framework;

// So it can use RobustServerSimulation.
[assembly: InternalsVisibleTo("Robust.Benchmarks")]
[assembly: InternalsVisibleTo("Robust.Server.IntegrationTests")]
[assembly: InternalsVisibleTo("Robust.Shared.IntegrationTests")]

[assembly: Parallelizable(ParallelScope.Fixtures)]
