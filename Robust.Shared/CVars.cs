using System;
using System.Runtime.InteropServices;
using System.Threading;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Network;

namespace Robust.Shared
{
    [CVarDefs]
    public abstract class CVars
    {
        protected CVars()
        {
            throw new InvalidOperationException("This class must not be instantiated");
        }

        /*
         * NET
         */

        public static readonly CVarDef<int> NetPort =
            CVarDef.Create("net.port", 1212, CVar.ARCHIVE);

        public static readonly CVarDef<int> NetSendBufferSize =
            CVarDef.Create("net.sendbuffersize", 131071, CVar.ARCHIVE);

        public static readonly CVarDef<int> NetReceiveBufferSize =
            CVarDef.Create("net.receivebuffersize", 131071, CVar.ARCHIVE);

        public static readonly CVarDef<bool> NetVerbose =
            CVarDef.Create("net.verbose", false);

        public static readonly CVarDef<string> NetServer =
            CVarDef.Create("net.server", "127.0.0.1", CVar.ARCHIVE | CVar.CLIENTONLY);

        public static readonly CVarDef<int> NetUpdateRate =
            CVarDef.Create("net.updaterate", 20, CVar.ARCHIVE | CVar.CLIENTONLY);

        public static readonly CVarDef<int> NetCmdRate =
            CVarDef.Create("net.cmdrate", 30, CVar.ARCHIVE | CVar.CLIENTONLY);

        public static readonly CVarDef<int> NetRate =
            CVarDef.Create("net.rate", 10240, CVar.ARCHIVE | CVar.CLIENTONLY);

        // That's comma-separated, btw.
        public static readonly CVarDef<string> NetBindTo =
            CVarDef.Create("net.bindto", "0.0.0.0,::", CVar.ARCHIVE | CVar.SERVERONLY);

        public static readonly CVarDef<bool> NetDualStack =
            CVarDef.Create("net.dualstack", false, CVar.ARCHIVE | CVar.SERVERONLY);

        public static readonly CVarDef<bool> NetInterp =
            CVarDef.Create("net.interp", true, CVar.ARCHIVE);

        public static readonly CVarDef<int> NetInterpRatio =
            CVarDef.Create("net.interp_ratio", 0, CVar.ARCHIVE);

        public static readonly CVarDef<bool> NetLogging =
            CVarDef.Create("net.logging", false, CVar.ARCHIVE);

        public static readonly CVarDef<bool> NetPredict =
            CVarDef.Create("net.predict", true, CVar.CLIENTONLY);

        public static readonly CVarDef<int> NetPredictTickBias =
            CVarDef.Create("net.predict_tick_bias", 1, CVar.CLIENTONLY);

        // On Windows we default this to 16ms lag bias, to account for time period lag in the Lidgren thread.
        // Basically due to how time periods work on Windows, messages are (at worst) time period-delayed when sending.
        // BUT! Lidgren's latency calculation *never* measures this due to how it works.
        // This broke some prediction calculations quite badly so we bias them to mask it.
        // This is not necessary on Linux because Linux, for better or worse,
        // just has the Lidgren thread go absolute brr polling.
        public static readonly CVarDef<float> NetPredictLagBias = CVarDef.Create(
                "net.predict_lag_bias",
                OperatingSystem.IsWindows() ? 0.016f : 0,
                CVar.CLIENTONLY);

        public static readonly CVarDef<int> NetStateBufMergeThreshold =
            CVarDef.Create("net.state_buf_merge_threshold", 5, CVar.ARCHIVE);

        public static readonly CVarDef<bool> NetPVS =
            CVarDef.Create("net.pvs", true, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        public static readonly CVarDef<float> StreamedTilesPerSecond =
            CVarDef.Create("net.stream_tps", 500f, CVar.ARCHIVE | CVar.SERVER);

        public static readonly CVarDef<float> StreamedTileRange =
            CVarDef.Create("net.stream_range", 15f, CVar.ARCHIVE | CVar.SERVER);

        public static readonly CVarDef<float> NetMaxUpdateRange =
            CVarDef.Create("net.maxupdaterange", 12.5f, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        public static readonly CVarDef<bool> NetLogLateMsg =
            CVarDef.Create("net.log_late_msg", true);

        public static readonly CVarDef<int> NetTickrate =
            CVarDef.Create("net.tickrate", 60, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        /**
         * SUS
         */

        public static readonly CVarDef<int> SysWinTickPeriod =
            CVarDef.Create("sys.win_tick_period", 3, CVar.SERVERONLY);

        // On non-FULL_RELEASE builds, use ProfileOptimization/tiered JIT to speed up game startup.
        public static readonly CVarDef<bool> SysProfileOpt =
            CVarDef.Create("sys.profile_opt", true);

        /// <summary>
        ///     Controls stack size of the game logic thread, in bytes.
        /// </summary>
        public static readonly CVarDef<int> SysGameThreadStackSize =
            CVarDef.Create("sys.game_thread_stack_size", 8 * 1024 * 1024);

        /// <summary>
        ///     Controls stack size of the game logic thread.
        /// </summary>
        public static readonly CVarDef<int> SysGameThreadPriority =
            CVarDef.Create("sys.game_thread_priority", (int) ThreadPriority.AboveNormal);

#if DEBUG
        public static readonly CVarDef<float> NetFakeLoss = CVarDef.Create("net.fakeloss", 0f, CVar.CHEAT);
        public static readonly CVarDef<float> NetFakeLagMin = CVarDef.Create("net.fakelagmin", 0f, CVar.CHEAT);
        public static readonly CVarDef<float> NetFakeLagRand = CVarDef.Create("net.fakelagrand", 0f, CVar.CHEAT);
        public static readonly CVarDef<float> NetFakeDuplicates = CVarDef.Create("net.fakeduplicates", 0f, CVar.CHEAT);
#endif

        /*
         * METRICS
         */

        public static readonly CVarDef<bool> MetricsEnabled =
            CVarDef.Create("metrics.enabled", false, CVar.SERVERONLY);

        public static readonly CVarDef<string> MetricsHost =
            CVarDef.Create("metrics.host", "localhost", CVar.SERVERONLY);

        public static readonly CVarDef<int> MetricsPort =
            CVarDef.Create("metrics.port", 44880, CVar.SERVERONLY);

        /*
         * STATUS
         */

        public static readonly CVarDef<bool> StatusEnabled =
            CVarDef.Create("status.enabled", true, CVar.ARCHIVE | CVar.SERVERONLY);

        public static readonly CVarDef<string> StatusBind =
            CVarDef.Create("status.bind", "*:1212", CVar.ARCHIVE | CVar.SERVERONLY);

        public static readonly CVarDef<int> StatusMaxConnections =
            CVarDef.Create("status.max_connections", 5, CVar.SERVERONLY);

        public static readonly CVarDef<string> StatusConnectAddress =
            CVarDef.Create("status.connectaddress", "", CVar.ARCHIVE | CVar.SERVERONLY);

        /*
         * BUILD
         */

        public static readonly CVarDef<string> BuildEngineVersion =
            CVarDef.Create("build.engine_version", "", CVar.SERVERONLY);

        public static readonly CVarDef<string> BuildForkId =
            CVarDef.Create("build.fork_id", "", CVar.SERVERONLY);

        public static readonly CVarDef<string> BuildVersion =
            CVarDef.Create("build.version", "", CVar.SERVERONLY);

        public static readonly CVarDef<string> BuildDownloadUrl =
            CVarDef.Create("build.download_url", string.Empty, CVar.SERVERONLY);

        public static readonly CVarDef<string> BuildHash =
            CVarDef.Create("build.hash", "", CVar.SERVERONLY);

        /*
         * WATCHDOG
         */

        public static readonly CVarDef<string> WatchdogToken =
            CVarDef.Create("watchdog.token", "", CVar.SERVERONLY);

        public static readonly CVarDef<string> WatchdogKey =
            CVarDef.Create("watchdog.key", "", CVar.SERVERONLY);

        public static readonly CVarDef<string> WatchdogBaseUrl =
            CVarDef.Create("watchdog.baseUrl", "http://localhost:5000", CVar.SERVERONLY);

        /*
         * GAME
         */

        public static readonly CVarDef<int> GameMaxPlayers =
            CVarDef.Create("game.maxplayers", 32, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        public static readonly CVarDef<string> GameHostName =
            CVarDef.Create("game.hostname", "MyServer", CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// If a grid is shrunk to include no more tiles should it be deleted.
        /// </summary>
        public static readonly CVarDef<bool> GameDeleteEmptyGrids =
            CVarDef.Create("game.delete_empty_grids", true, CVar.ARCHIVE | CVar.SERVER);

        /*
         * LOG
         */

        public static readonly CVarDef<bool> LogEnabled =
            CVarDef.Create("log.enabled", true, CVar.ARCHIVE | CVar.SERVERONLY);

        public static readonly CVarDef<string> LogPath =
            CVarDef.Create("log.path", "logs", CVar.ARCHIVE | CVar.SERVERONLY);

        public static readonly CVarDef<string> LogFormat =
            CVarDef.Create("log.format", "log_%(date)s-T%(time)s.txt", CVar.ARCHIVE | CVar.SERVERONLY);

        public static readonly CVarDef<LogLevel> LogLevel =
            CVarDef.Create("log.level", Log.LogLevel.Info, CVar.ARCHIVE | CVar.SERVERONLY);

        public static readonly CVarDef<bool> LogRuntimeLog =
            CVarDef.Create("log.runtimelog", true, CVar.ARCHIVE | CVar.SERVERONLY);

        /*
         * Light
         */

        /// <summary>
        /// This is the maximum the viewport is enlarged to check for any intersecting render-trees for lights.
        /// This should be set to your maximum light radius.
        /// </summary>
        /// <remarks>
        /// If this value is too small it just means there may be pop-in where a light is located on a render-tree
        /// outside of our viewport.
        /// </remarks>
        public static readonly CVarDef<float> MaxLightRadius =
            CVarDef.Create("light.max_radius", 32.1f, CVar.CLIENTONLY);

        /*
         * Lookup
         */

        /// <summary>
        /// Like MaxLightRadius this is how far we enlarge lookups to find intersecting components.
        /// This should be set to your maximum entity size.
        /// </summary>
        public static readonly CVarDef<float> LookupEnlargementRange =
            CVarDef.Create("lookup.enlargement_range", 10.0f, CVar.ARCHIVE | CVar.REPLICATED | CVar.CHEAT);

        /*
         * LOKI
         */

        public static readonly CVarDef<bool> LokiEnabled =
            CVarDef.Create("loki.enabled", false, CVar.SERVERONLY);

        public static readonly CVarDef<string> LokiName =
            CVarDef.Create("loki.name", "", CVar.SERVERONLY);

        public static readonly CVarDef<string> LokiAddress =
            CVarDef.Create("loki.address", "", CVar.SERVERONLY);

        public static readonly CVarDef<string> LokiUsername =
            CVarDef.Create("loki.username", "", CVar.SERVERONLY);

        public static readonly CVarDef<string> LokiPassword =
            CVarDef.Create("loki.password", "", CVar.SERVERONLY);

        /*
         * AUTH
         */

        public static readonly CVarDef<int> AuthMode =
            CVarDef.Create("auth.mode", (int) Network.AuthMode.Optional, CVar.SERVERONLY);

        public static readonly CVarDef<bool> AuthAllowLocal =
            CVarDef.Create("auth.allowlocal", true, CVar.SERVERONLY);

        // Only respected on server, client goes through IAuthManager for security.
        public static readonly CVarDef<string> AuthServer =
            CVarDef.Create("auth.server", AuthManager.DefaultAuthServer, CVar.SERVERONLY);

        /*
         * DISPLAY
         */

        public static readonly CVarDef<bool> DisplayVSync =
            CVarDef.Create("display.vsync", true, CVar.ARCHIVE | CVar.CLIENTONLY);

        public static readonly CVarDef<int> DisplayWindowMode =
            CVarDef.Create("display.windowmode", 0, CVar.ARCHIVE | CVar.CLIENTONLY);

        public static readonly CVarDef<int> DisplayWidth =
            CVarDef.Create("display.width", 1280, CVar.CLIENTONLY);

        public static readonly CVarDef<int> DisplayHeight =
            CVarDef.Create("display.height", 720, CVar.CLIENTONLY);

        public static readonly CVarDef<int> DisplayLightMapDivider =
            CVarDef.Create("display.lightmapdivider", 2, CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<int> DisplayMaxLightsPerScene =
            CVarDef.Create("display.maxlightsperscene", 128, CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<bool> DisplaySoftShadows =
            CVarDef.Create("display.softshadows", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<bool> DisplayBlurLight =
            CVarDef.Create("display.blur_light", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<float> DisplayBlurLightFactor =
            CVarDef.Create("display.blur_light_factor", 0.001f, CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<bool> DisplayBlurFov =
            CVarDef.Create("display.blur_fov", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<float> DisplayBlurFovFactor =
            CVarDef.Create("display.blur_fov_factor", 0.0008f, CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<float> DisplayUIScale =
            CVarDef.Create("display.uiScale", 0f, CVar.ARCHIVE | CVar.CLIENTONLY);

        // Clyde related enums are in Clyde.Constants.cs.

        /// <summary>
        /// Which renderer to use to render the game.
        /// </summary>
        public static readonly CVarDef<int> DisplayRenderer =
            CVarDef.Create("display.renderer", 0, CVar.CLIENTONLY);

        /// <summary>
        /// Whether to use compatibility mode.
        /// </summary>
        /// <remarks>
        /// This can change certain behaviors like GL version selection to try to avoid driver crashes.
        /// </remarks>
        public static readonly CVarDef<bool> DisplayCompat =
            CVarDef.Create("display.compat", false, CVar.CLIENTONLY);

        /// <summary>
        /// Which OpenGL version to use for the OpenGL renderer.
        /// </summary>
        public static readonly CVarDef<int> DisplayOpenGLVersion =
            CVarDef.Create("display.opengl_version", 0, CVar.CLIENTONLY);

        /// <summary>
        /// On Windows, use ANGLE as OpenGL implementation.
        /// </summary>
        public static readonly CVarDef<bool> DisplayAngle =
            CVarDef.Create("display.angle", true, CVar.CLIENTONLY);

        /// <summary>
        /// Use a custom swap chain when using ANGLE.
        /// Should improve performance and fixes main window sRGB handling with ANGLE.
        /// </summary>
        public static readonly CVarDef<bool> DisplayAngleCustomSwapChain =
            CVarDef.Create("display.angle_custom_swap_chain", true, CVar.CLIENTONLY);

        /// <summary>
        /// Force usage of DXGI 1.1 when using custom swap chain setup.
        /// </summary>
        public static readonly CVarDef<bool> DisplayAngleDxgi1 =
            CVarDef.Create("display.angle_dxgi1", false, CVar.CLIENTONLY);

        /// <summary>
        /// Try to use the display adapter with this name, if the current renderer supports selecting it.
        /// </summary>
        public static readonly CVarDef<string> DisplayAdapter =
            CVarDef.Create("display.adapter", "", CVar.CLIENTONLY);

        /// <summary>
        /// Use EGL to create GL context instead of GLFW, if possible.
        /// </summary>
        /// <remarks>
        /// This only tries to use EGL if on a platform like X11 or Windows (w/ ANGLE) where it is possible.
        /// </remarks>
        public static readonly CVarDef<bool> DisplayEgl =
            CVarDef.Create("display.egl", true, CVar.CLIENTONLY);

        public static readonly CVarDef<int> DisplayFontDpi =
            CVarDef.Create("display.fontdpi", 96, CVar.CLIENTONLY);

        /// <summary>
        /// Override detected OpenGL version, for testing.
        /// </summary>
        public static readonly CVarDef<string> DisplayOGLOverrideVersion =
            CVarDef.Create("display.ogl_override_version", string.Empty, CVar.CLIENTONLY);

        /// <summary>
        /// Run <c>glCheckError()</c> after (almost) every GL call.
        /// </summary>
        public static readonly CVarDef<bool> DisplayOGLCheckErrors =
            CVarDef.Create("display.ogl_check_errors", false, CVar.CLIENTONLY);

        /// <summary>
        ///     Forces synchronization of multi-window rendering with <c>glFinish</c> when GL fence sync is unavailable.
        /// </summary>
        /// <remarks>
        ///     If this is disabled multi-window rendering on GLES2 might run better, dunno.
        ///     It technically causes UB thanks to the OpenGL spec with cross-context sync. Hope that won't happen.
        ///     Let's be real the OpenGL specification is basically just a suggestion to drivers anyways so who cares.
        /// </remarks>
        public static readonly CVarDef<bool> DisplayForceSyncWindows =
            CVarDef.Create<bool>("display.force_sync_windows", true, CVar.CLIENTONLY);

        /// <summary>
        /// Use a separate thread for multi-window blitting.
        /// </summary>
        public static readonly CVarDef<bool> DisplayThreadWindowBlit =
            CVarDef.Create("display.thread_window_blit", true, CVar.CLIENTONLY);

        public static readonly CVarDef<int> DisplayInputBufferSize =
            CVarDef.Create("display.input_buffer_size", 32, CVar.CLIENTONLY);

        public static readonly CVarDef<bool> DisplayWin32Experience =
            CVarDef.Create("display.win32_experience", false, CVar.CLIENTONLY);

        /// <summary>
        /// The window icon set to use. Overriden by <c>GameControllerOptions</c> on startup.
        /// </summary>
        /// <remarks>
        /// Dynamically changing this does nothing.
        /// </remarks>
        public static readonly CVarDef<string> DisplayWindowIconSet =
            CVarDef.Create("display.window_icon_set", "", CVar.CLIENTONLY);

        /// <summary>
        /// The splash logo to use. Overriden by <c>GameControllerOptions</c> on startup.
        /// </summary>
        /// <remarks>
        /// Dynamically changing this does nothing.
        /// </remarks>
        public static readonly CVarDef<string> DisplaySplashLogo =
            CVarDef.Create("display.splash_logo", "", CVar.CLIENTONLY);

        /*
         * AUDIO
         */

        public static readonly CVarDef<int> AudioAttenuation =
            CVarDef.Create("audio.attenuation", (int) Attenuation.Default, CVar.REPLICATED | CVar.ARCHIVE);

        public static readonly CVarDef<string> AudioDevice =
            CVarDef.Create("audio.device", string.Empty, CVar.CLIENTONLY);

        public static readonly CVarDef<float> AudioMasterVolume =
            CVarDef.Create("audio.mastervolume", 1.0f, CVar.ARCHIVE | CVar.CLIENTONLY);

        /*
         * PLAYER
         */

        public static readonly CVarDef<string> PlayerName =
            CVarDef.Create("player.name", "JoeGenero", CVar.ARCHIVE | CVar.CLIENTONLY);

        /*
         * PHYSICS
         */

        // Grid fixtures
        /// <summary>
        /// I'ma be real with you: the only reason this exists is to get tests working.
        /// </summary>
        public static readonly CVarDef<bool> GenerateGridFixtures =
            CVarDef.Create("physics.grid_fixtures", true, CVar.REPLICATED);

        // - Contacts
        public static readonly CVarDef<int> ContactMultithreadThreshold =
            CVarDef.Create("physics.contact_multithread_threshold", 32);

        public static readonly CVarDef<int> ContactMinimumThreads =
            CVarDef.Create("physics.contact_minimum_threads", 2);

        // - Sleep
        public static readonly CVarDef<float> AngularSleepTolerance =
            CVarDef.Create("physics.angsleeptol", 2.0f / 180.0f * MathF.PI);

        public static readonly CVarDef<float> LinearSleepTolerance =
            CVarDef.Create("physics.linsleeptol", 0.001f);

        public static readonly CVarDef<bool> SleepAllowed =
            CVarDef.Create("physics.sleepallowed", true);

        // Box2D default is 0.5f
        public static readonly CVarDef<float> TimeToSleep =
            CVarDef.Create("physics.timetosleep", 0.2f);

        // - Solver
        public static readonly CVarDef<int> PositionConstraintsPerThread =
            CVarDef.Create("physics.position_constraints_per_thread", 32);

        public static readonly CVarDef<int> PositionConstraintsMinimumThread =
            CVarDef.Create("physics.position_constraints_minimum_threads", 2);

        public static readonly CVarDef<int> VelocityConstraintsPerThread =
            CVarDef.Create("physics.velocity_constraints_per_thread", 32);

        public static readonly CVarDef<int> VelocityConstraintMinimumThreads =
            CVarDef.Create("physics.velocity_constraints_minimum_threads", 2);

        // These are the minimum recommended by Box2D with the standard being 8 velocity 3 position iterations.
        // Trade-off is obviously performance vs how long it takes to stabilise.
        // PhysX opts for fewer velocity iterations and more position but they also have a different solver.
        public static readonly CVarDef<int> PositionIterations =
            CVarDef.Create("physics.positer", 3);

        public static readonly CVarDef<int> VelocityIterations =
            CVarDef.Create("physics.veliter", 8);

        public static readonly CVarDef<bool> WarmStarting =
            CVarDef.Create("physics.warmstart", true);

        public static readonly CVarDef<bool> AutoClearForces =
            CVarDef.Create("physics.autoclearforces", true);

        /// <summary>
        /// A velocity threshold for elastic collisions. Any collision with a relative linear
        /// velocity below this threshold will be treated as inelastic.
        /// </summary>
        public static readonly CVarDef<float> VelocityThreshold =
            CVarDef.Create("physics.velocitythreshold", 0.5f);

        // TODO: Copy Box2D's comments on baumgarte I think it's on the solver class.
        /// <summary>
        ///     How much overlap is resolved per tick.
        /// </summary>
        public static readonly CVarDef<float> Baumgarte =
            CVarDef.Create("physics.baumgarte", 0.2f);

        /// <summary>
        /// A small length used as a collision and constraint tolerance. Usually it is
        /// chosen to be numerically significant, but visually insignificant.
        /// </summary>
        /// <remarks>
        ///     Note that some joints may have this cached and not update on value change.
        /// </remarks>
        public static readonly CVarDef<float> LinearSlop =
            CVarDef.Create("physics.linearslop", 0.005f);

        /// <summary>
        /// A small angle used as a collision and constraint tolerance. Usually it is
        /// chosen to be numerically significant, but visually insignificant.
        /// </summary>
        public static readonly CVarDef<float> AngularSlop =
            CVarDef.Create("physics.angularslop", 2.0f / 180.0f * MathF.PI);

        /// <summary>
        /// If true, it will run a GiftWrap convex hull on all polygon inputs.
        /// This makes for a more stable engine when given random input,
        /// but if speed of the creation of polygons are more important,
        /// you might want to set this to false.
        /// </summary>
        public static readonly CVarDef<bool> ConvexHullPolygons =
            CVarDef.Create("physics.convexhullpolygons", true);

        public static readonly CVarDef<int> MaxPolygonVertices =
            CVarDef.Create("physics.maxpolygonvertices", 8);

        public static readonly CVarDef<float> MaxLinearCorrection =
            CVarDef.Create("physics.maxlinearcorrection", 0.2f);

        public static readonly CVarDef<float> MaxAngularCorrection =
            CVarDef.Create("physics.maxangularcorrection", 8.0f / 180.0f * MathF.PI);

        // - Maximums
        /// <summary>
        /// Maximum linear velocity per second.
        /// Make sure that MaxLinVelocity / <see cref="NetTickrate"/> is around 0.5 or higher so that moving objects don't go through walls.
        /// MaxLinVelocity is compared to the dot product of linearVelocity * frameTime.
        /// </summary>
        /// <remarks>
        /// Default is 35 m/s. Around half a tile per tick at 60 ticks per second.
        /// </remarks>
        public static readonly CVarDef<float> MaxLinVelocity =
            CVarDef.Create("physics.maxlinvelocity", 35f);

        /// <summary>
        /// Maximum angular velocity in full rotations per second.
        /// MaxAngVelocity is compared to the squared rotation.
        /// </summary>
        /// <remarks>
        /// Default is 15 rotations per second. Approximately a quarter rotation per tick at 60 ticks per second.
        /// </remarks>
        public static readonly CVarDef<float> MaxAngVelocity =
            CVarDef.Create("physics.maxangvelocity", 15f);

        /*
         * DISCORD
         */

        public static readonly CVarDef<bool> DiscordEnabled =
            CVarDef.Create("discord.enabled", true, CVar.CLIENTONLY);

        /*
         * RES
         */

        public static readonly CVarDef<bool> ResCheckPathCasing =
            CVarDef.Create("res.checkpathcasing", false);

        public static readonly CVarDef<bool> ResTexturePreloadingEnabled =
            CVarDef.Create("res.texturepreloadingenabled", true, CVar.CLIENTONLY);

        public static readonly CVarDef<bool> ResTexturePreloadCache =
            CVarDef.Create("res.texture_preload_cache", true, CVar.CLIENTONLY);


        /*
         * DEBUG
         */

        /// <summary>
        ///     Target framerate for things like the frame graph.
        /// </summary>
        public static readonly CVarDef<int> DebugTargetFps =
            CVarDef.Create("debug.target_fps", 60, CVar.CLIENTONLY | CVar.ARCHIVE);

        /*
         * MIDI
         */

        public static readonly CVarDef<float> MidiVolume =
            CVarDef.Create("midi.volume", 0f, CVar.CLIENTONLY | CVar.ARCHIVE);
    }
}
