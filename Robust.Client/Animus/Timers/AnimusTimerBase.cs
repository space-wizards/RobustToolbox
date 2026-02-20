using System;
using JetBrains.Annotations;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Client.Animus.Timers;

[ImplicitDataDefinitionForInheritors]
[PublicAPI]
public abstract partial class AnimusTimerBase
{
    public abstract TimeSpan GetNextPeriod(IRobustRandom random);
}
