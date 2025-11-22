#if TOOLS

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Stylesheets;
using Robust.Shared.Asynchronous;
using Robust.Shared.IoC;

[assembly: MetadataUpdateHandler(typeof(EngineStylesheetReload))]

namespace Robust.Client.UserInterface.Stylesheets;

internal static class EngineStylesheetReload
{
    private static readonly List<WeakReference<IDependencyCollection>> Instances = [];

    internal static void RegisterForReload(IDependencyCollection clientInstance)
    {
        Instances.Add(new WeakReference<IDependencyCollection>(clientInstance));
    }

    internal static void UnregisterForReload(IDependencyCollection clientInstance)
    {
        Instances.RemoveAll(x => !x.TryGetTarget(out var target) || target == clientInstance);
    }

    [UsedImplicitly]
    private static void ClearCache(Type[]? updatedTypes)
    {
        foreach (var instance in Instances)
        {
            if (!instance.TryGetTarget(out var target))
                continue;

            var taskManager = target.Resolve<ITaskManager>();
            var styleSheetManager = target.Resolve<IEngineStylesheetManagerInternal>();

            taskManager.RunOnMainThread(() =>
            {
                styleSheetManager.Reload();
            });
        }
    }
}

#endif
