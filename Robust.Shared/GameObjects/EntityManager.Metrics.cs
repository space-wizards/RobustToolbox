using System.Diagnostics.Metrics;
using Robust.Shared.IoC;

namespace Robust.Shared.GameObjects;

public abstract partial class EntityManager
{
    [Dependency] private readonly IMeterFactory _meterFactory = null!;

    private void InitMetrics()
    {
        var meter = _meterFactory.Create("Robust.EntityManager");
        meter.CreateObservableUpDownCounter("entity_count", () => Entities.Count);
    }
}
