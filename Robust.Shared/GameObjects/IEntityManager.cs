using JetBrains.Annotations;
using Prometheus;

namespace Robust.Shared.GameObjects;

[PublicAPI]
public interface IEntityManager : IEntitySystemManager
{
    void Initialize();

    void Startup();

    void TickUpdate(float frameTime, Histogram? histogram = null);

    void FrameUpdate(float frameTime);

    void Shutdown();
}

internal class EntityManager : EntitySystemManager, IEntityManager { }
