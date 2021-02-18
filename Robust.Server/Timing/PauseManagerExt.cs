using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Robust.Server.Timing
{
    public static class PauseManagerExt
    {
        [Pure]
        [Obsolete("Use IEntity.Paused directly")]
        public static bool IsEntityPaused(this IPauseManager manager, IEntity entity)
        {
            return entity.Paused;
        }
    }
}
