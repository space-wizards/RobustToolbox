using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Robust.Client.UserInterface.Controllers;

/// <summary>
///     Each <see cref="UIController"/> is instantiated as a singleton by <see cref="UserInterfaceManager"/>
///     <see cref="UIController"/> can use <see cref="DependencyAttribute"/> for regular IoC dependencies
///     and <see cref="UISystemDependencyAttribute"/> to depend on <see cref="EntitySystem"/>s, which will be automatically
///     injected once they are created.
/// </summary>
public abstract partial class UIController : IPostInjectInit
{
    [Dependency] protected readonly IUserInterfaceManager UIManager = default!;
    [Dependency] protected readonly IEntitySystemManager EntitySystemManager = default!;
    [Dependency] protected readonly IEntityManager EntityManager = default!;
    [Dependency] protected readonly ILogManager LogManager = default!;

    public ISawmill Log { get; protected set; } = default!;

    public virtual void Initialize()
    {
    }

    public virtual void FrameUpdate(FrameEventArgs args)
    {
    }

    protected virtual string SawmillName
    {
        get
        {
            var name = GetType().Name;

            // Strip trailing "UIController"
            if (name.EndsWith("UIController"))
                name = name.Substring(0, name.Length - "UIController".Length);

            // Convert CamelCase to snake_case
            // Ignore if all uppercase, assume acronym (e.g. NPC or HTN)
            if (name.All(char.IsUpper))
            {
                name = name.ToLower(CultureInfo.InvariantCulture);
            }
            else
            {
                name = string.Concat(name.Select(x => char.IsUpper(x) ? $"_{char.ToLower(x)}" : x.ToString()));
                name = name.Trim('_');
            }

            return $"ui.{name}";
        }
    }

    public void PostInject()
    {
        Log = LogManager.GetSawmill(SawmillName);

#if !DEBUG
        Log.Level = LogLevel.Info;
#endif
    }
}
