using System;
using System.Collections.Generic;
using System.Diagnostics;
using Robust.Client.ResourceManagement;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Stopwatch = Robust.Shared.Timing.Stopwatch;

namespace Robust.Client.Graphics;

public interface ILoadingScreenManager
{
    void BeginLoadingSection(string sectionName);

    /// <summary>
    /// Start a loading bar "section" for the given method.
    /// Must be ended with EndSection.
    /// </summary>
    void BeginLoadingSection(object method);

    void EndLoadingSection();

    /// <summary>
    /// Will run the giving function and add a custom "section" for it on the loading screen.
    /// </summary>
    void LoadingStep(Action action, object method);
}

/// <summary>
/// Manager that creates and displays a basic splash screen and loading bar.
/// </summary>
internal sealed class LoadingScreenManager : ILoadingScreenManager
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IClydeInternal _clyde = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;

    private readonly Stopwatch _sw = new();

    #region UI constants

    private const int LoadingBarWidth = 250;
    private const int LoadingBarHeight = 20;
    private const int LoadingBarOutlineOffset = 5;
    private static readonly Vector2i LogoLoadingBarOffset = (0, 20);
    private static readonly Vector2i LoadTimesIndent = (20, 0);

    private const int NumLongestLoadTimes = 5;

    private static readonly Color LoadingBarColor = Color.White;

    #endregion

    #region Cvars

    private string SplashLogo = "";
    private bool ShowLoadingBar;

    #endregion

    private const string FontLocation = "/EngineFonts/NotoSans/NotoSans-Regular.ttf";
    private const int FontSize = 11;
    private VectorFont? _font;

    // Number of loading sections for the loading bar. This has to be manually set!
    private int _numberOfLoadingSections;

    // The name of the section and how much time it took to load
    private readonly List<(string Name, TimeSpan LoadTime)> _times = [];

    private int _currentSection;
    private string? _currentSectionName;

    private bool _currentlyInSection;
    private bool _finished;

    public void Initialize(int sections)
    {
        if (_finished)
            return;

        _clyde.VsyncEnabled = false;

        _numberOfLoadingSections = sections;

        _sawmill = _logManager.GetSawmill("loading");

        SplashLogo = _cfg.GetCVar(CVars.DisplaySplashLogo);
        ShowLoadingBar = _cfg.GetCVar(CVars.DisplayShowLoadingBar);

        if (_resourceCache.TryGetResource<FontResource>(FontLocation, out var fontResource))
            _font = new VectorFont(fontResource, FontSize);
        else
            _sawmill.Error($"Could not load font: {FontLocation}");
    }

    public void BeginLoadingSection(string sectionName)
    {
        if (_finished)
            return;

        if (_currentlyInSection)
            throw new Exception("You cannot begin more than one section at a time!");

        _currentlyInSection = true;

        _currentSectionName = sectionName;
        // This ensures that if the screen was resized or something the new size is properly updated to clyde.
        _clyde.ProcessInput(new FrameEventArgs((float) _sw.Elapsed.TotalSeconds));
        _sw.Restart();
        _clyde.Render();
    }

    /// <summary>
    /// Start a loading bar "section" for the given method.
    /// Must be ended with EndSection.
    /// </summary>
    public void BeginLoadingSection(object method)
    {
        if (_finished)
            return;

        BeginLoadingSection(method.GetType().Name);
    }

    public void EndLoadingSection()
    {
        if (_finished)
            return;

        var time = _sw.Elapsed;
        if (_currentSectionName != null)
            _times.Add((_currentSectionName, time));
        _currentSection++;
        _currentlyInSection = false;
    }

    /// <summary>
    /// Will run the giving function and add a custom "section" for it on the loading screen.
    /// </summary>
    public void LoadingStep(Action action, object method)
    {
        if (_finished)
            return;

        BeginLoadingSection(method.GetType().Name);
        action();
        EndLoadingSection();
    }

    public void Finish()
    {
        if (_finished)
            return;

        _clyde.VsyncEnabled = _cfg.GetCVar(CVars.DisplayVSync);

        if (_currentSection != _numberOfLoadingSections)
            _sawmill.Error($"The number of seen loading sections isn't equal to the total number of loading sections! Seen: {_currentSection}, Total: {_numberOfLoadingSections}");

        _finished = true;
    }

    #region Drawing functions

    /// <summary>
    /// Draw out the splash and loading screen.
    /// </summary>
    public void DrawLoadingScreen(IRenderHandle handle, Vector2i screenSize)
    {
        if (_finished)
            return;

        // Start at the center!
        var location = screenSize / 2;

        DrawSplash(handle, ref location);

        DrawLoadingBar(handle, ref location);

        DrawCurrentLoading(handle, ref location);

        DrawTopTimes(handle, ref location);
    }

    private void DrawSplash(IRenderHandle handle, ref Vector2i startLocation)
    {
        if (!_resourceCache.TryGetResource<TextureResource>(SplashLogo, out var textureResource))
            return;

        handle.DrawingHandleScreen.DrawTexture(textureResource.Texture, startLocation - textureResource.Texture.Size / 2);
        startLocation += Vector2i.Up * textureResource.Texture.Size.Y / 2;
    }

    private void DrawLoadingBar(IRenderHandle handle, ref Vector2i location)
    {
        // Always do the offsets, it looks a lot better!
        location += Vector2i.Left * LoadingBarWidth / 2;
        location += LogoLoadingBarOffset;

        if (!ShowLoadingBar)
            return;

        var sectionWidth = LoadingBarWidth / _numberOfLoadingSections;

        var barTopLeft = location;
        var barBottomRight = new Vector2i(_currentSection * sectionWidth % LoadingBarWidth, LoadingBarHeight);
        var barBottomRightMax = new Vector2i(LoadingBarWidth, LoadingBarHeight);

        var outlinePosition = barTopLeft + Vector2i.DownLeft * LoadingBarOutlineOffset;
        var outlineSize = barBottomRightMax + Vector2i.UpRight * 2 * LoadingBarOutlineOffset;

        // Outline
        handle.DrawingHandleScreen.DrawRect(UIBox2.FromDimensions(outlinePosition, outlineSize), LoadingBarColor, false);

        // Progress bar
        handle.DrawingHandleScreen.DrawRect(UIBox2.FromDimensions(barTopLeft, barBottomRight), LoadingBarColor);

        location += Vector2i.Up * outlineSize;
    }

    // Draw the currently loading section to the screen.
    [Conditional("TOOLS")]
    private void DrawCurrentLoading(IRenderHandle handle, ref Vector2i location)
    {
        if (_font == null || _currentSectionName == null)
            return;

        handle.DrawingHandleScreen.DrawString(_font, location, _currentSectionName);
        location += Vector2i.Up * _font.GetLineHeight(1.0f);
    }

    // Draw the slowest loading times to the screen.
    [Conditional("TOOLS")]
    private void DrawTopTimes(IRenderHandle handle, ref Vector2i location)
    {
        if (_font == null)
            return;

        location += LoadTimesIndent;

        var offset = 0;
        var x = 0;
        _times.Sort((a, b) => b.LoadTime.CompareTo(a.LoadTime));

        foreach (var val in _times)
        {
            if (x >= NumLongestLoadTimes)
                break;

            var entry = $"{val.LoadTime.TotalSeconds:F2} - {val.Name}";
            handle.DrawingHandleScreen.DrawString(_font, location + new Vector2i(0, offset), entry);
            offset += _font.GetLineHeight(1.0f);
            x++;
        }

        location += Vector2i.Up * offset;
    }

    #endregion // Drawing functions
}
