using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Robust.Shared.Scripting
{
    [PublicAPI]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public abstract class ScriptGlobalsShared
    {
        [field: Dependency] public IEntityManager ent { get; } = default!;
        [field: Dependency] public IComponentManager comp { get; } = default!;
        [field: Dependency] public IPrototypeManager prot { get; } = default!;

        public IEnumerable<T> protos<T>() where T : class, IPrototype
        {
            return prot.EnumeratePrototypes<T>();
        }

        public IEnumerable<EntityPrototype> eprotos => prot.EnumeratePrototypes<EntityPrototype>();

        public GridCoordinates gpos(double x, double y, int gridId)
        {
            return new GridCoordinates((float)x, (float)y, new GridId(gridId));
        }

        public GridCoordinates gpos(double x, double y, GridId gridId)
        {
            return new GridCoordinates((float)x, (float)y, gridId);
        }

        public IEntity spawn(string prototype, GridCoordinates position)
        {
            return ent.SpawnEntity(prototype, position);
        }

        public T res<T>()
        {
            return IoCManager.Resolve<T>();
        }

        public abstract void write(object toString);
        public abstract void show(object obj);
    }
}
