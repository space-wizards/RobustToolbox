using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Client.Editor.Styling;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;

namespace Robust.Client.UserInterface.Stylesheets;

public interface IEngineStylesheetManager
{
    /// <summary>
    /// Apply a stylesheet to a control and automatically subscribe to updates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This will automatically update the stylesheet on the control if the backing stylesheet changed,
    /// for example due to user preferences.
    /// </para>
    /// <para>
    /// A call to <see cref="UseStylesheet"/> should always be paired with a call to <see cref="StopStylesheet"/>,
    /// otherwise memory leaks will ensue! The best way to do this is to call <see cref="UseStylesheet"/> in
    /// <see cref="Control.EnteredTree"/>, and call <see cref="StopStylesheet"/> in <see cref="Control.ExitedTree"/>.
    /// </para>
    /// <para>
    /// If this method gets called twice on the same control, it will simply replace the previous
    /// <paramref name="getStylesheet"/> method. In this scenario, <see cref="StopStylesheet"/> does <b>not</b> need to
    /// be called another time for cleanup, in this scenario.
    /// </para>
    /// </remarks>
    /// <param name="control">The control to apply the stylesheet to.</param>
    /// <param name="getStylesheet">
    /// A function used to select the stylesheet from the <see cref="IEngineStylesheetAccessor"/>.
    /// </param>
    void UseStylesheet(Control control, Func<IEngineStylesheetAccessor, Stylesheet?> getStylesheet);

    /// <summary>
    /// Stop stylesheet update subscription from <see cref="UseStylesheet"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This does not (currently) unset the stylesheet on <paramref name="control"/>, as a performance optimization.
    /// Do not rely on this.
    /// </para>
    /// </remarks>
    /// <param name="control">The control to unsubscribe.</param>
    void StopStylesheet(Control control);
}

public static class StylesheetAccessorExt
{
    public static Stylesheet? GetOrNull(this IEngineStylesheetAccessor accessor, string name)
    {
        if (accessor.TryGetStylesheet(name, out var sheet))
            return sheet;

        return null;
    }
}

internal interface IEngineStylesheetManagerInternal : IEngineStylesheetManager
{
    void Initialize();
    void Shutdown();
    void Reload();
}

public interface IEngineStylesheetAccessor
{
    Stylesheet SheetDefault { get; }
    bool TryGetStylesheet(string name, [MaybeNullWhen(false)] out Stylesheet stylesheet);
}

internal sealed class EngineStylesheetManager : IEngineStylesheetManagerInternal
{
    [Dependency] private readonly IUserInterfaceManager _uiMgr = null!;
    [Dependency] private readonly IResourceCache _res = null!;
    [Dependency] private readonly IDependencyCollection _deps = null!;

    private readonly Dictionary<Control, Func<IEngineStylesheetAccessor, Stylesheet?>> _controlStylesheetSubs = [];
    private readonly Dictionary<string, Stylesheet> _stylesheets = [];
    private readonly Accessor _accessor;

    public EngineStylesheetManager()
    {
        _accessor = new Accessor(this);
    }

    public void Initialize()
    {
        Reload();

        _uiMgr.Stylesheet = _stylesheets[DefaultStylesheet.Name];

#if TOOLS
        EngineStylesheetReload.RegisterForReload(_deps);
#endif
    }

    public void Shutdown()
    {
#if TOOLS
        EngineStylesheetReload.UnregisterForReload(_deps);
#endif
    }

    public void Reload()
    {
        _stylesheets.Clear();

        var styleDefault = new DefaultStylesheet(_res, _uiMgr);
        _stylesheets.Add(DefaultStylesheet.Name, styleDefault.Stylesheet);
        var editorDark = new EditorDarkStylesheet(new BaseEngineStylesheet.NoConfig());
        _stylesheets.Add(EditorDarkStylesheet.Name, editorDark.Stylesheet);
        var editorLight = new EditorLightStylesheet(new BaseEngineStylesheet.NoConfig());
        _stylesheets.Add(EditorLightStylesheet.Name, editorLight.Stylesheet);

        foreach (var (control, getter) in _controlStylesheetSubs)
        {
            control.Stylesheet = getter(_accessor);
        }
    }

    public void UseStylesheet(Control control, Func<IEngineStylesheetAccessor, Stylesheet?> getStylesheet)
    {
        _controlStylesheetSubs[control] = getStylesheet;
        control.Stylesheet = getStylesheet(_accessor);
    }

    public void StopStylesheet(Control control)
    {
        _controlStylesheetSubs.Remove(control);
    }

    private sealed class Accessor(EngineStylesheetManager owner) : IEngineStylesheetAccessor
    {
        public Stylesheet SheetDefault => owner._stylesheets[DefaultStylesheet.Name];

        public bool TryGetStylesheet(string name, [MaybeNullWhen(false)] out Stylesheet stylesheet)
        {
            return owner._stylesheets.TryGetValue(name, out stylesheet);
        }
    }
}

