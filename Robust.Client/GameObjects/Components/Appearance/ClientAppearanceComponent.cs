using System.Collections.Generic;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects;

/// <summary>
/// This is the client instance of <see cref="AppearanceComponent"/>.
/// </summary>
[RegisterComponent]
[ComponentReference(typeof(AppearanceComponent)), Friend(typeof(AppearanceSystem))]
public sealed class ClientAppearanceComponent : AppearanceComponent
{
    [ViewVariables]
    [DataField("visuals")]
    internal List<AppearanceVisualizer> Visualizers = new();
}
