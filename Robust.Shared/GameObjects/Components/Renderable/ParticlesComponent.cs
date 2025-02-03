using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects;

[RegisterComponent, NetworkedComponent]
public sealed partial class ParticlesComponent : Component {
    [ViewVariables] string ParticleType;
}
