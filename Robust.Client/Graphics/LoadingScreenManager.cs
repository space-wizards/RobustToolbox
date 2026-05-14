using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Stopwatch = Robust.Shared.Timing.Stopwatch;

namespace Robust.Client.Graphics;

internal interface ILoadingScreenManager
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
internal sealed partial class LoadingScreenManager : ILoadingScreenManager
{
    [Dependency] private IResourceCache _resourceCache = default!;
    [Dependency] private IClydeInternal _clyde = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private ILogManager _logManager = default!;

    private ISawmill _sawmill = default!;

    private readonly Stopwatch _sw = new();

    #region UI constants

    private const int LoadingBarWidth = 340;
    private const int LoadingBarHeight = 18;
    private const int LoadingBarOutlineOffset = 3;
    private const int NumLongestLoadTimes = 6;

    private const string EclipseBackground = "/Textures/Loading/eclipse-loading-background.png";
    private const string EclipseLoadingLogo = "/Textures/Logo/eclipse-icon-loading.png";
    private const string EclipseTitle = "E C L I P S E   S T A T I O N";

    private static readonly Color LoadingBarColor = Color.FromHex("#ffc55d");
    private static readonly Color LoadingBarTrackColor = Color.FromHex("#0b0906cc");
    private static readonly Color EclipseGold = Color.FromHex("#ffc55d");
    private static readonly Color EclipseGoldDim = Color.FromHex("#8a6025");
    private static readonly Color EclipseText = Color.FromHex("#bda879");

    #endregion

    #region Cvars

    private string _splashLogo = "";
    private bool _showLoadingBar;
    private bool _showDebug;

    #endregion

    private const string FontLocation = "/EngineFonts/NotoSans/NotoSans-Regular.ttf";
    private const int FontSize = 11;
    private VectorFont? _font;

    // Number of loading sections for the loading bar. This has to be manually set!
    private int _numberOfLoadingSections;

    // The name of the section and how much time it took to load
    internal readonly List<(string Name, TimeSpan LoadTime)> Times = [];

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

        _splashLogo = _cfg.GetCVar(CVars.DisplaySplashLogo);
        _showLoadingBar = _cfg.GetCVar(CVars.LoadingShowBar);
        _showDebug = _cfg.GetCVar(CVars.LoadingShowDebug);

        if (_resourceCache.TryGetResource<FontResource>(FontLocation, out var fontResource))
            _font = new VectorFont(fontResource, FontSize);
        else
            _sawmill.Error($"Could not load font: {FontLocation}");
    }

    public void BeginLoadingSection(string sectionName) => BeginLoadingSection(sectionName, false);
    public void BeginLoadingSection(string sectionName, bool dontRender)
    {
        if (_finished)
            return;

        if (_currentlyInSection)
            throw new InvalidOperationException("You cannot begin more than one section at a time!");

        _currentlyInSection = true;

        _currentSectionName = sectionName;

        if (_clyde.IsInitialized)
        {
            _clyde.MainWindow.SetWindowProgress(
                WindowProgressState.Normal,
                _currentSection / (float)_numberOfLoadingSections);
        }

        if (!dontRender)
        {
            // This ensures that if the screen was resized or something the new size is properly updated to clyde.
            _clyde.ProcessInput(new FrameEventArgs((float)_sw.Elapsed.TotalSeconds));
            _sw.Restart();
            _clyde.Render();
        }
        else
        {
            _sw.Restart();
        }
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
            Times.Add((_currentSectionName, time));
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

        BeginLoadingSection(method as string ?? method.GetType().Name);
        action();
        EndLoadingSection();
    }

    public void Finish()
    {
        if (_finished)
            return;

        if (_currentSection != _numberOfLoadingSections)
            _sawmill.Error($"The number of seen loading sections isn't equal to the total number of loading sections! Seen: {_currentSection}, Total: {_numberOfLoadingSections}");

        _clyde.MainWindow.SetWindowProgress(WindowProgressState.None, 1);

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

        var scale = UserInterfaceManager.CalculateUIScale(_clyde.MainWindow.ContentScale.X, _cfg);
        scale *= Math.Clamp(screenSize.Y / 1080f, 1f, 1.35f);
        var screen = new UIBox2(0, 0, screenSize.X, screenSize.Y);

        var shadeWidth = screenSize.X * 0.337f;
        DrawEclipseBackground(handle, screenSize, screen, shadeWidth);

        var panelRightPadding = 42f * scale;
        var panelLeft = Math.Clamp(screenSize.X * 0.07f, 42f * scale, MathF.Max(42f * scale, shadeWidth - LoadingBarWidth * scale - panelRightPadding));
        var panelWidth = MathF.Min(LoadingBarWidth * scale, MathF.Max(180f * scale, shadeWidth - panelLeft - panelRightPadding));
        var panelTop = MathF.Max(64f * scale, screenSize.Y * 0.31f);
        var location = new Vector2(panelLeft, panelTop);

        DrawSplash(handle, ref location, scale);
        DrawTitle(handle, ref location, scale, panelWidth);
        DrawLoadingBar(handle, ref location, scale, panelWidth);
        DrawCurrentLoading(handle, ref location, scale, panelWidth);
        DrawTopTimes(handle, ref location, scale, panelWidth);
    }

    private void DrawEclipseBackground(IRenderHandle handle, Vector2i screenSize, UIBox2 screen, float shadeWidth)
    {
        var screenHandle = handle.DrawingHandleScreen;

        if (_resourceCache.TryGetResource<TextureResource>(EclipseBackground, out var background))
        {
            var texture = background.Texture;
            var textureSize = (Vector2) texture.Size;
            var coverScale = MathF.Max(screenSize.X / textureSize.X, screenSize.Y / textureSize.Y);
            var drawSize = textureSize * coverScale;
            var drawPos = ((Vector2) screenSize - drawSize) / 2f;

            screenHandle.DrawTextureRect(texture, UIBox2.FromDimensions(drawPos, drawSize));
        }
        else
        {
            screenHandle.DrawRect(screen, Color.Black);
        }

        screenHandle.DrawRect(screen, Color.FromHex("#00000040"));
        screenHandle.DrawRect(new UIBox2(0, 0, shadeWidth, screenSize.Y), Color.FromHex("#00000066"));
    }

    private void DrawSplash(IRenderHandle handle, ref Vector2 startLocation, float scale)
    {
        if (string.IsNullOrEmpty(_splashLogo))
            return;

        if (!_resourceCache.TryGetResource<TextureResource>(EclipseLoadingLogo, out var textureResource) &&
            !_resourceCache.TryGetResource<TextureResource>(_splashLogo, out textureResource))
            return;

        var maxLogoSize = 120f * scale;
        var textureSize = (Vector2) textureResource.Texture.Size;
        var logoScale = maxLogoSize / MathF.Max(textureSize.X, textureSize.Y);
        var drawSize = textureSize * logoScale;
        var drawPosition = startLocation;

        handle.DrawingHandleScreen.DrawTextureRect(textureResource.Texture, UIBox2.FromDimensions(drawPosition, drawSize));

        startLocation.Y += drawSize.Y + 34f * scale;
    }

    private void DrawTitle(IRenderHandle handle, ref Vector2 location, float scale, float panelWidth)
    {
        if (_font == null)
            return;

        var maxTitleWidth = panelWidth * 0.98f;
        var titleScale = 2.1f * scale;
        var titleSize = handle.DrawingHandleScreen.GetDimensions(_font, EclipseTitle, titleScale);
        if (titleSize.X > maxTitleWidth)
            titleScale *= maxTitleWidth / titleSize.X;

        handle.DrawingHandleScreen.DrawString(_font, location, EclipseTitle, titleScale, EclipseGold);
        location.Y += _font.GetLineHeight(titleScale) + 18f * scale;

        var lineY = location.Y;
        var lineWidth = MathF.Min(panelWidth, LoadingBarWidth * scale);
        handle.DrawingHandleScreen.DrawRect(new UIBox2(location.X, lineY, location.X + lineWidth, lineY + 1f * scale), EclipseGoldDim);
        handle.DrawingHandleScreen.DrawCircle(new Vector2(location.X + lineWidth * 0.5f, lineY + 0.5f * scale), 3f * scale, EclipseGold);
        location.Y += 28f * scale;
    }

    private void DrawLoadingBar(IRenderHandle handle, ref Vector2 location, float scale, float panelWidth)
    {
        var barWidth = (int)(LoadingBarWidth * scale);
        var barHeight = (int)(LoadingBarHeight * scale);
        var outlineOffset = (int)(LoadingBarOutlineOffset * scale);

        if (!_showLoadingBar)
            return;

        barWidth = (int) MathF.Min(barWidth, panelWidth);
        var progress = _numberOfLoadingSections <= 0
            ? 0f
            : Math.Clamp(_currentSection / (float) _numberOfLoadingSections, 0f, 1f);

        var barTopLeft = location;
        var barSize = new Vector2(barWidth, barHeight);
        var progressSize = new Vector2(barWidth * progress, barHeight);

        var outlinePosition = barTopLeft + new Vector2(-outlineOffset, -outlineOffset);
        var outlineSize = barSize + new Vector2(outlineOffset * 2, outlineOffset * 2);

        handle.DrawingHandleScreen.DrawRect(UIBox2.FromDimensions(outlinePosition, outlineSize), EclipseGoldDim, false);
        handle.DrawingHandleScreen.DrawRect(UIBox2.FromDimensions(barTopLeft, barSize), LoadingBarTrackColor);
        handle.DrawingHandleScreen.DrawRect(UIBox2.FromDimensions(barTopLeft, progressSize), LoadingBarColor);

        location.Y += outlineSize.Y + 22f * scale;
    }

    // Draw the currently loading section to the screen.
    private void DrawCurrentLoading(IRenderHandle handle, ref Vector2 location, float scale, float panelWidth)
    {
        if (_font == null || _currentSectionName == null)
            return;

        var titleScale = 1.15f * scale;
        handle.DrawingHandleScreen.DrawString(_font, location, "LOADING SYSTEMS", titleScale, EclipseGold);

        var dots = "...";
        var dotsSize = handle.DrawingHandleScreen.GetDimensions(_font, dots, titleScale);
        handle.DrawingHandleScreen.DrawString(_font, new Vector2(location.X + MathF.Min(panelWidth, LoadingBarWidth * scale) - dotsSize.X, location.Y), dots, titleScale, EclipseGold);

        location.Y += _font.GetLineHeight(titleScale) + 18f * scale;
    }

    // Draw the slowest loading times to the screen.
    private void DrawTopTimes(IRenderHandle handle, ref Vector2 location, float scale, float panelWidth)
    {
        if (_font == null || !_showDebug)
            return;

        var entries = Times
            .Where(x => x.Name != "Texture preload")
            .TakeLast(NumLongestLoadTimes)
            .Reverse()
            .ToList();

        var offset = 0;
        for (var x = 0; x < entries.Count; x++)
        {
            var (name, time) = entries[x];

            DrawLoadingEntry(handle, location + new Vector2(0, offset), name, (float) time.TotalSeconds / 100f, scale, x == 0, x < entries.Count - 1, panelWidth);
            offset += _font.GetLineHeight(scale);
        }

        location.Y += offset;
    }

    private void DrawLoadingEntry(IRenderHandle handle, Vector2 location, string name, float value, float scale, bool active, bool hasNext, float panelWidth)
    {
        if (_font == null)
            return;

        var clamped = Math.Clamp(value, 0f, 1f);
        var color = active ? EclipseGold : EclipseText;
        var markerColor = active ? LoadingBarColor : EclipseGoldDim;
        var percent = $"[{clamped * 100f:0.00}%]";
        var percentSize = handle.DrawingHandleScreen.GetDimensions(_font, percent, scale);
        var right = location.X + MathF.Min(panelWidth, LoadingBarWidth * scale);

        var markerCenter = location + new Vector2(5f * scale, 8f * scale);
        if (hasNext)
        {
            var dashWidth = MathF.Max(1f, 1.3f * scale);
            var dashHeight = MathF.Max(3f, 4f * scale);
            handle.DrawingHandleScreen.DrawRect(new UIBox2(
                markerCenter.X - dashWidth / 2f,
                markerCenter.Y + 6f * scale,
                markerCenter.X + dashWidth / 2f,
                markerCenter.Y + 6f * scale + dashHeight),
                EclipseGoldDim);
        }

        handle.DrawingHandleScreen.DrawCircle(markerCenter, 3f * scale, markerColor);
        handle.DrawingHandleScreen.DrawString(_font, location + new Vector2(22f * scale, 0), name, scale, color);
        handle.DrawingHandleScreen.DrawString(_font, new Vector2(right - percentSize.X, location.Y), percent, scale, EclipseGold);
    }

    #endregion // Drawing functions
}

internal sealed partial class ShowTopLoadingTimesCommand : IConsoleCommand
{
    [Dependency] private LoadingScreenManager _mgr = default!;

    public string Command => "loading_top";
    public string Description => "";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var sorted = _mgr.Times.Where(x => x.LoadTime > TimeSpan.FromSeconds(0.01)).OrderByDescending(x => x.LoadTime);
        foreach (var (name, time) in sorted)
        {
            shell.WriteLine($"{time.TotalSeconds:F2} - {name}");
        }
    }
}
