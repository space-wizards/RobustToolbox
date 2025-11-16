using System;
using JetBrains.Annotations;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;

namespace Robust.Client.UserInterface.Stylesheets;

// This is a cut down version of SS14's sheetlet-based stylesheet system.

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[MeansImplicitUse]
internal sealed class EngineSheetletAttribute : Attribute;

internal abstract class EngineSheetlet<T>
{
    [Dependency] protected readonly IResourceCache ResCache = null!;

    protected EngineSheetlet()
    {
        IoCManager.InjectDependencies(this);
    }

    public abstract StyleRule[] GetRules(T sheet, object config);
}
