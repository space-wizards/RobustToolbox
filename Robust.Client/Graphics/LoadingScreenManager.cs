using System;
using System.Collections.Generic;
using Robust.Client.ResourceManagement;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Client.Graphics;

/// <summary>
/// Manager that creates and displays a basic splash screen and loading bar.
/// </summary>
public sealed partial class LoadingScreenManager
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IClydeInternal _clyde = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;

    private readonly Stopwatch _sw = new();

    #region UI constants

    // The x and y location of the loading bar as a percentage of the screen width.
    private const float LoadingBarLocationX = 0.5f;
    private const float LoadingBarLocationY = 0.675f;

    private const int LoadingBarWidth = 250;
    private const int LoadingBarHeight = 20;
    private const int LoadingBarOutlineOffset = 5;
    private static readonly Vector2i DefaultOffset = (0, 10);
    private static readonly Vector2i TopTimesExtraOffset = (20, 10);
    private const int TopTimesSpacing = 15;

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

    /// <summary>
    /// Draw out the splash and loading screen.
    /// </summary>
    public void DrawLoadingScreen(IRenderHandle handle, Vector2i screenSize)
    {
        if (_finished)
            return;

        DrawSplash(handle, screenSize);

        var center = new Vector2i((int) Math.Round(screenSize.X * LoadingBarLocationX), (int) Math.Round(screenSize.Y * LoadingBarLocationY));
        var startLocation = center - new Vector2i(LoadingBarWidth/2, 0);

        DrawLoadingBar(handle, ref startLocation);

#if DEBUG
        DrawCurrentLoading(handle, ref startLocation);
        DrawTopTimes(handle, ref startLocation);
#endif
    }

    private void DrawSplash(IRenderHandle handle, Vector2i screenSize)
    {
        if (string.IsNullOrEmpty(SplashLogo))
            return;

        var texture = _resourceCache.GetResource<TextureResource>(SplashLogo).Texture;

        handle.DrawingHandleScreen.DrawTexture(texture, (screenSize - texture.Size) / 2);
    }

    private void DrawLoadingBar(IRenderHandle handle, ref Vector2i startLocation)
    {
        if (!ShowLoadingBar)
            return;

        var sectionWidth = LoadingBarWidth / _numberOfLoadingSections;

        var barTopLeft = startLocation;
        var barBottomRight = startLocation + new Vector2i(_currentSection * sectionWidth % LoadingBarWidth, -LoadingBarHeight);

        // Outline
        handle.DrawingHandleScreen.DrawRect(new UIBox2
            {
                TopLeft = barTopLeft + Vector2i.UpLeft * LoadingBarOutlineOffset,
                BottomRight = startLocation + new Vector2i(LoadingBarWidth, -LoadingBarHeight) + Vector2i.DownRight * LoadingBarOutlineOffset,
            },
            Color.White,
            false);

        // Progress bar
        handle.DrawingHandleScreen.DrawRect(new UIBox2
            {
                TopLeft = barTopLeft,
                BottomRight = barBottomRight,
            },
            LoadingBarColor);

        startLocation += DefaultOffset;
    }

    // Draw the currently loading section to the screen.
    private void DrawCurrentLoading(IRenderHandle handle, ref Vector2i startLocation)
    {
        if (_font == null || _currentSectionName == null)
            return;

        handle.DrawingHandleScreen.DrawString(_font, startLocation, _currentSectionName);
        startLocation += DefaultOffset;
    }

    // Draw the slowest loading times to the screen.
    private void DrawTopTimes(IRenderHandle handle, ref Vector2i startLocation)
    {
        if (_font == null)
            return;

        startLocation += TopTimesExtraOffset;

        var offset = 0;
        var x = 0;
        _times.Sort((a, b) => b.LoadTime.CompareTo(a.LoadTime));

        foreach (var val in _times)
        {
            if (x >= NumLongestLoadTimes)
                break;

            var entry = $"{val.LoadTime:ss\\.ff} - {val.Name}";
            handle.DrawingHandleScreen.DrawString(_font, startLocation + new Vector2i(0, offset), entry);
            offset += TopTimesSpacing;
            x++;
        }
    }
}

