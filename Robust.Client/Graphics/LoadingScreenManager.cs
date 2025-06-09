using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Client.ResourceManagement;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Random;
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

    private readonly Stopwatch _sw = new();

    private const int LoadingBarWidth = 250;
    private const int LoadingBarHeight = 20;
    private int LoadingBarMaxSections = 10;
    private const int NumLongestLoadTimes = 5;

    private List<Color> Colors =
    [
        Color.White,
        Color.LightCoral,
        Color.LightGreen,
        Color.LightSkyBlue
    ];

    #region Cvars

    private string SplashLogo = "";
    private float LoadingXLocation;
    private float LoadingYLocation;
    private bool ShowLoadingBar;
    private bool ShowCurrentLoadingSection;
    private bool ShowLoadTimes;
    private int SeenNumberOfLoadingSections;

    #endregion

    private FontResource? _font;

    private Color _loadingBarColor;

    private readonly List<(string Name, TimeSpan LoadTime)> _times = [];

    private int _currentSection = 1;

    private string _currentSectionName = "";

    public void Initialize()
    {
        SplashLogo = _cfg.GetCVar(CVars.DisplaySplashLogo);
        LoadingXLocation = _cfg.GetCVar(CVars.DisplayLoadingXLocation);
        LoadingYLocation = _cfg.GetCVar(CVars.DisplayLoadingYLocation);
        ShowLoadingBar = _cfg.GetCVar(CVars.DisplayShowLoadingBar);
        ShowCurrentLoadingSection = _cfg.GetCVar(CVars.DisplayShowCurrentLoadingSection);
        ShowLoadTimes = _cfg.GetCVar(CVars.DisplayShowLoadTimes);
        SeenNumberOfLoadingSections = _cfg.GetCVar(CVars.SeenNumberOfLoadingSections);

        LoadingBarMaxSections = SeenNumberOfLoadingSections == 0 ? LoadingBarMaxSections : SeenNumberOfLoadingSections;

        _resourceCache.TryGetResource("/EngineFonts/NotoSans/NotoSans-Regular.ttf", out _font);

        _loadingBarColor = Random.Shared.Pick(Colors);
    }

    public void BeginLoadingSection(string sectionName)
    {
        _sw.Restart();
        _currentSectionName = sectionName;
        _clyde.ProcessInput();
        _clyde.Render();
    }

    /// <summary>
    /// Start a loading bar "section" for the given method.
    /// Must be ended with EndSection.
    /// </summary>
    public void BeginLoadingSection(object method)
    {
        BeginLoadingSection(method.GetType().Name);
    }

    public void EndLoadingSection()
    {
        var time = _sw.Elapsed;
        _times.Add((_currentSectionName, time));
        _currentSection++;
    }

    public void Finish()
    {
        if (!_cfg.HasLoadedConfiguration())
            return;

        _cfg.SetCVar(CVars.SeenNumberOfLoadingSections, _currentSection);
        _cfg.SaveToFile();
    }

    /// <summary>
    /// Will run the giving function and add a custom "section" for it on the loading screen.
    /// </summary>
    public void LoadingStep(Action action, object method)
    {
        BeginLoadingSection(method.GetType().Name);
        action();
        EndLoadingSection();
    }

    /// <summary>
    /// Draw out the splash and loading screen.
    /// </summary>
    public void DrawLoadingScreen(IRenderHandle handle, Vector2i screenSize)
    {
        DrawSplash(handle, screenSize);

        var startLocation = new Vector2i((int) Math.Round(screenSize.X * LoadingXLocation), (int) Math.Round(screenSize.Y * LoadingYLocation));

        DrawLoadingBar(handle, ref startLocation);
        DrawCurrentLoading(handle, ref startLocation);
        DrawTopTimes(handle, ref startLocation);
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

        startLocation -= new Vector2i(LoadingBarWidth/2, 0);
        var sectionWidth = LoadingBarWidth / LoadingBarMaxSections;

        var barTopLeft = startLocation;
        var barBottomRight = startLocation + new Vector2i(_currentSection * sectionWidth % LoadingBarWidth, -LoadingBarHeight);
        var outlineOffset = 5;

        // Outline
        handle.DrawingHandleScreen.DrawRect(new UIBox2
            {
                TopLeft = barTopLeft - new Vector2i(outlineOffset, -outlineOffset),
                BottomRight = startLocation + new Vector2i(LoadingBarWidth, -LoadingBarHeight) + new Vector2i(outlineOffset, -outlineOffset),
            },
            Color.White,
            false);

        // Progress bar
        handle.DrawingHandleScreen.DrawRect(new UIBox2
            {
                TopLeft = barTopLeft,
                BottomRight = barBottomRight,
            },
            _loadingBarColor);

        startLocation += new Vector2i(0, 10);
    }

    private void DrawCurrentLoading(IRenderHandle handle, ref Vector2i startLocation)
    {
        if (_font == null || !ShowCurrentLoadingSection)
            return;

        handle.DrawingHandleScreen.DrawString(new VectorFont(_font, 11), startLocation, _currentSectionName);
        startLocation += new Vector2i(0, 10);
    }

    private void DrawTopTimes(IRenderHandle handle, ref Vector2i startLocation)
    {
        if (_font == null || !ShowLoadTimes)
            return;

        startLocation += new Vector2i(20, 10);

        var offset = 0;
        var x = 0;
        _times.Sort((a, b) => b.LoadTime.CompareTo(a.LoadTime));
        foreach (var val in _times)
        {
            if (x >= NumLongestLoadTimes)
                break;

            var time = val.LoadTime.ToString(@"ss\.ff");
            var entry = $"{time} - {val.Name}";
            handle.DrawingHandleScreen.DrawString(new VectorFont(_font, 10), startLocation + new Vector2i(0, offset), entry);
            offset += 13;
            x++;
        }
    }
}

