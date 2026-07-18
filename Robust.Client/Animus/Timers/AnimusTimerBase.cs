using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Client.Animus.Timers;

[ImplicitDataDefinitionForInheritors]
[PublicAPI]
public abstract partial class AnimusTimerBase
{
    protected IRobustRandom Random { get; private set; }

    /// <summary>
    /// IoCManager.InjectDependencies doesn't work, override this method to initialize dependencies manually.
    /// </summary>
    /// <param name="entityManager">Entity manager to fetch systems with.</param>
    /// <param name="dependencyCollection">Dependency collection to fetch non-system dependencies with.</param>
    public virtual void Initialize(IEntityManager entityManager, IDependencyCollection dependencyCollection)
    {
        Random = dependencyCollection.Resolve<IRobustRandom>();
    }

    public abstract TimeSpan GetNextPeriod();
}
