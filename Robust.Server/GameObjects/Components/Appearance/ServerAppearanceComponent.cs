using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects;

/// <summary>
/// This is the server instance of <see cref="AppearanceComponent"/>.
/// </summary>
[RegisterComponent]
[ComponentReference(typeof(AppearanceComponent))]
public sealed partial class ServerAppearanceComponent : AppearanceComponent { }
