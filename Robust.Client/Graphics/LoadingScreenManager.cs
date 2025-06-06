using System;
using System.Collections.Generic;
using Robust.Client.ResourceManagement;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
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

    private readonly Stopwatch _sw = new();

    private int _currentSection = 1;

    private const int LoadingBarWidth = 250;
    private const int LoadingBarHeight = 20;
    private const int LoadingBarMaxSections = 10;
    private const int NumLongestLoadTimes = 5;

    #region Cvars

    private string SplashLogo = "";
    private float LoadingXLocation;
    private float LoadingYLocation;
    private bool ShowLoadingBar;
    private bool ShowCurrentLoadingSection;
    private bool ShowLoadTimes;

    #endregion

    private FontResource? Font;

    private List<Color> Colors =
    [
        Color.LightCoral,
        Color.LightGreen,
        Color.LightSkyBlue,
    ];

    private List<(string Name, TimeSpan LoadTime)> Times = [];

    private string CurrentSectionName = "";

    public void Initialize()
    {
        SplashLogo = _cfg.GetCVar(CVars.DisplaySplashLogo);
        LoadingXLocation = _cfg.GetCVar(CVars.DisplayLoadingXLocation);
        LoadingYLocation = _cfg.GetCVar(CVars.DisplayLoadingYLocation);
        ShowLoadingBar = _cfg.GetCVar(CVars.DisplayShowLoadingBar);
        ShowCurrentLoadingSection = _cfg.GetCVar(CVars.DisplayShowCurrentLoadingSection);
        ShowLoadTimes = _cfg.GetCVar(CVars.DisplayShowLoadTimes);

        _resourceCache.TryGetResource("/EngineFonts/NotoSans/NotoSans-Regular.ttf", out Font);
    }

    public void BeginLoadingSection(string sectionName)
    {
        _sw.Restart();
        CurrentSectionName = sectionName;
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
        Times.Add((CurrentSectionName, time));
        _currentSection++;
    }

    /// <summary>
    /// Start a specific
    /// </summary>
    public void DisplayLoadingStep(Action action, object method)
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
        handle.DrawingHandleScreen.DrawRect(new UIBox2
            {
                BottomRight = startLocation + new Vector2i(_currentSection%LoadingBarMaxSections * sectionWidth, -LoadingBarHeight),
                TopLeft = startLocation,
            },
            Colors[_currentSection / LoadingBarMaxSections % Colors.Count]);

        startLocation += new Vector2i(0, 10);
    }

    private void DrawCurrentLoading(IRenderHandle handle, ref Vector2i startLocation)
    {
        if (Font == null || !ShowCurrentLoadingSection)
            return;

        handle.DrawingHandleScreen.DrawString(new VectorFont(Font, 11), startLocation, CurrentSectionName);
        startLocation += new Vector2i(0, 10);
    }

    private void DrawTopTimes(IRenderHandle handle, ref Vector2i startLocation)
    {
        if (Font == null || !ShowLoadTimes)
            return;

        startLocation += new Vector2i(20, 10);

        var offset = 0;
        var x = 0;
        Times.Sort((a, b) => b.LoadTime.CompareTo(a.LoadTime));
        foreach (var val in Times)
        {
            if (x >= NumLongestLoadTimes)
                break;

            var time = val.LoadTime.ToString(@"ss\.ff");
            var entry = $"{time} - {val.Name}";
            handle.DrawingHandleScreen.DrawString(new VectorFont(Font, 10), startLocation + new Vector2i(0, offset), entry);
            offset += 13;
            x++;
        }
    }
}


