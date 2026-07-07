using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Client.Animus.Triggers;

[ImplicitDataDefinitionForInheritors]
[PublicAPI]
public sealed partial class AnimusTriggerEvents : AnimusTriggerBase
{
    [DataField]
    public List<string> Events { get; set; }
}
