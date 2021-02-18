using System;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
#if DEBUG
    // If you wanna use these, add it to some random prototype.
    // I recommend the #1 mug:
    // 1. it doesn't spawn on the map (currently).
    // 2. it's at the top of the entity list (currently).
    // 3. it crashed the game before, it's iconic!

    /// <summary>
    /// Throws an exception in <see cref="OnAdd" />.
    /// </summary>
    public sealed class DebugExceptionOnAddComponent : Component
    {
        public override string Name => "DebugExceptionOnAdd";

        public override void OnAdd() => throw new NotSupportedException();
    }

    /// <summary>
    /// Throws an exception in <see cref="ExposeData" />.
    /// </summary>
    public sealed class DebugExceptionExposeDataComponent : Component
    {
        public override string Name => "DebugExceptionExposeData";

        public override void ExposeData(ObjectSerializer serializer) => throw new NotSupportedException();
    }

    /// <summary>
    /// Throws an exception in <see cref="Initialize" />.
    /// </summary>
    public sealed class DebugExceptionInitializeComponent : Component
    {
        public override string Name => "DebugExceptionInitialize";

        public override void Initialize() => throw new NotSupportedException();
    }

    /// <summary>
    /// Throws an exception in <see cref="Startup" />.
    /// </summary>
    public sealed class DebugExceptionStartupComponent : Component
    {
        public override string Name => "DebugExceptionStartup";

        protected override void Startup() => throw new NotSupportedException();
    }
#endif
}
