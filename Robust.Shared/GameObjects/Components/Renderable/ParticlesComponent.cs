using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects;

[NetworkedComponent]
public abstract partial class SharedYamlParticlesComponent : Component {
    [ViewVariables] public string ParticleType;
}
