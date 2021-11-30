using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    /// <inheritdoc />
    public sealed class Entity : IEntity
    {
        public Entity(IEntityManager entityManager, EntityUid uid) : base(entityManager, uid)
        {
        }
    }
}
