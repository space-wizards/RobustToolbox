using System;
using System.Runtime.InteropServices;
using System.Threading;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Physics;

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

        /// <summary>
        /// UDP port to bind to for main game networking.
        /// Each address specified in <c>net.bindto</c> is bound with this port.
        /// </summary>
        public static readonly CVarDef<int> NetPort =
            CVarDef.Create("net.port", 1212, CVar.ARCHIVE);

        /// <summary>
        /// Send buffer size on the UDP sockets used for main game networking.
        /// </summary>
        public static readonly CVarDef<int> NetSendBufferSize =
            CVarDef.Create("net.sendbuffersize", 131071, CVar.ARCHIVE);

        /// <summary>
        /// Receive buffer size on the UDP sockets used for main game networking.
        /// </summary>
        public static readonly CVarDef<int> NetReceiveBufferSize =
            CVarDef.Create("net.receivebuffersize", 131071, CVar.ARCHIVE);

        /// <summary>
        /// Whether to enable verbose debug logging in Lidgren.
        /// </summary>
        public static readonly CVarDef<bool> NetVerbose =
            CVarDef.Create("net.verbose", false);

        /// <summary>
        /// Comma-separated list of IP addresses to bind to for the main game networking port.
        /// The port bound is the value of <c>net.port</c> always.
        /// </summary>
        public static readonly CVarDef<string> NetBindTo =
            CVarDef.Create("net.bindto", "0.0.0.0,::", CVar.ARCHIVE | CVar.SERVERONLY);

        /// <summary>
        /// Whether to bind IPv6 sockets in dual-stack mode (for main game networking).
        /// </summary>
        public static readonly CVarDef<bool> NetDualStack =
            CVarDef.Create("net.dualstack", false, CVar.ARCHIVE | CVar.SERVERONLY);

        /// <summary>
        /// Whether to interpolate between server game states for render frames on the client.
        /// </summary>
        public static readonly CVarDef<bool> NetInterp =
            CVarDef.Create("net.interp", true, CVar.ARCHIVE);

        /// <summary>
        /// The target number of game states to keep buffered up to smooth out against network inconsistency.
        /// </summary>
        public static readonly CVarDef<int> NetInterpRatio =
            CVarDef.Create("net.interp_ratio", 0, CVar.ARCHIVE);

        /// <summary>
        /// Enable verbose game state/networking logging.
        /// </summary>
        public static readonly CVarDef<bool> NetLogging =
            CVarDef.Create("net.logging", false, CVar.ARCHIVE);

        /// <summary>
        /// Whether prediction is enabled on the client.
        /// </summary>
        /// <remarks>
        /// If off, simulation input commands will not fire and most entity methods will not run update.
        /// </remarks>
        public static readonly CVarDef<bool> NetPredict =
            CVarDef.Create("net.predict", true, CVar.CLIENTONLY);

        /// <summary>
        /// Extra amount of ticks to run-ahead for prediction on the client.
        /// </summary>
        public static readonly CVarDef<int> NetPredictTickBias =
            CVarDef.Create("net.predict_tick_bias", 1, CVar.CLIENTONLY);

        // On Windows we default this to 16ms lag bias, to account for time period lag in the Lidgren thread.
        // Basically due to how time periods work on Windows, messages are (at worst) time period-delayed when sending.
        // BUT! Lidgren's latency calculation *never* measures this due to how it works.
        // This broke some prediction calculations quite badly so we bias them to mask it.
        // This is not necessary on Linux because Linux, for better or worse,
        // just has the Lidgren thread go absolute brr polling.
        /// <summary>
        /// Extra amount of seconds to run-ahead for prediction on the client.
        /// </summary>
        public static readonly CVarDef<float> NetPredictLagBias = CVarDef.Create(
                "net.predict_lag_bias",
                OperatingSystem.IsWindows() ? 0.016f : 0,
                CVar.CLIENTONLY);

        public static readonly CVarDef<int> NetStateBufMergeThreshold =
            CVarDef.Create("net.state_buf_merge_threshold", 5, CVar.ARCHIVE);

        /// <summary>
        /// Whether to cull entities sent to clients from the server.
        /// If this is on, only entities immediately close to a client will be sent.
        /// Otherwise, all entities will be sent to all clients.
        /// </summary>
        public static readonly CVarDef<bool> NetPVS =
            CVarDef.Create("net.pvs", true, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// View size to take for PVS calculations,
        /// as the size of the sides of a square centered on the view points of clients.
        /// </summary>
        public static readonly CVarDef<float> NetMaxUpdateRange =
            CVarDef.Create("net.maxupdaterange", 12.5f, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// The amount of new entities that can be sent to a client in a single game state, under PVS.
        /// </summary>
        public static readonly CVarDef<int> NetPVSNewEntityBudget =
            CVarDef.Create("net.pvs_new_budget", 20, CVar.ARCHIVE | CVar.REPLICATED);

        /// <summary>
        /// The amount of entered entities that can be sent to a client in a single game state, under PVS.
        /// </summary>
        public static readonly CVarDef<int> NetPVSEntityBudget =
            CVarDef.Create("net.pvs_budget", 50, CVar.ARCHIVE | CVar.REPLICATED);

        /// <summary>
        /// Log late input messages from clients.
        /// </summary>
        public static readonly CVarDef<bool> NetLogLateMsg =
            CVarDef.Create("net.log_late_msg", true);

        /// <summary>
        /// Ticks per second on the server.
        /// This influences both how frequently game code processes, and how frequently updates are sent to clients.
        /// </summary>
        public static readonly CVarDef<int> NetTickrate =
            CVarDef.Create("net.tickrate", 60, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// How many seconds after the last message from the server before we consider it timed out.
        /// </summary>
        public static readonly CVarDef<float> ConnectionTimeout =
            CVarDef.Create("net.connection_timeout", 25.0f, CVar.ARCHIVE | CVar.CLIENTONLY);

        /// <summary>
        /// When doing the connection handshake, how long to wait before initial connection attempt packets.
        /// </summary>
        public static readonly CVarDef<float> ResendHandshakeInterval =
            CVarDef.Create("net.handshake_interval", 3.0f, CVar.ARCHIVE | CVar.CLIENTONLY);

        /// <summary>
        /// When doing the connection handshake, how many times to try sending initial connection attempt packets.
        /// </summary>
        public static readonly CVarDef<int> MaximumHandshakeAttempts =
            CVarDef.Create("net.handshake_attempts", 5, CVar.ARCHIVE | CVar.CLIENTONLY);

        /// <summary>
        /// If true, encrypt connections when possible.
        /// </summary>
        /// <remarks>
        /// Encryption is currently only possible when the client has authenticated with the auth server.
        /// </remarks>
        public static readonly CVarDef<bool> NetEncrypt =
            CVarDef.Create("net.encrypt", true, CVar.CLIENTONLY);

        /// <summary>
        /// If true, use UPnP to automatically forward ports on startup if possible.
        /// </summary>
        public static readonly CVarDef<bool> NetUPnP =
            CVarDef.Create("net.upnp", false, CVar.SERVERONLY);

        /// <summary>
        /// App identifier used by Lidgren. This must match between client and server for them to be able to connect.
        /// </summary>
        public static readonly CVarDef<string> NetLidgrenAppIdentifier =
            CVarDef.Create("net.lidgren_app_identifier", "RobustToolbox");

        /**
         * SUS
         */

        /// <summary>
        /// If not zero on Windows, the server will sent the tick period for its own process via <c>TimeBeginPeriod</c>.
        /// This increases polling and sleep precision of the network and main thread,
        /// but may negatively affect battery life or such.
        /// </summary>
        public static readonly CVarDef<int> SysWinTickPeriod =
            CVarDef.Create("sys.win_tick_period", 3, CVar.SERVERONLY);

        /// <summary>
        /// On non-FULL_RELEASE builds, use ProfileOptimization/tiered JIT to speed up game startup.
        /// </summary>
        public static readonly CVarDef<bool> SysProfileOpt =
            CVarDef.Create("sys.profile_opt", true);

        /// <summary>
        ///     Controls stack size of the game logic thread, in bytes.
        /// </summary>
        public static readonly CVarDef<int> SysGameThreadStackSize =
            CVarDef.Create("sys.game_thread_stack_size", 8 * 1024 * 1024);

        /// <summary>
        ///     Controls thread priority of the game logic thread.
        /// </summary>
        public static readonly CVarDef<int> SysGameThreadPriority =
            CVarDef.Create("sys.game_thread_priority", (int) ThreadPriority.AboveNormal);

#if DEBUG
        /// <summary>
        /// Add random fake network loss to all outgoing UDP network packets, as a ratio of how many packets to drop.
        /// 0 = no packet loss, 1 = all packets dropped
        /// </summary>
        public static readonly CVarDef<float> NetFakeLoss = CVarDef.Create("net.fakeloss", 0f, CVar.CHEAT);

        /// <summary>
        /// Add fake extra delay to all outgoing UDP network packets, in seconds.
        /// </summary>
        /// <seealso cref="NetFakeLagRand"/>
        public static readonly CVarDef<float> NetFakeLagMin = CVarDef.Create("net.fakelagmin", 0f, CVar.CHEAT);

        /// <summary>
        /// Add fake extra random delay to all outgoing UDP network packets, in seconds.
        /// The actual delay added for each packet is random between 0 and the specified value.
        /// </summary>
        /// <seealso cref="NetFakeLagMin"/>
        public static readonly CVarDef<float> NetFakeLagRand = CVarDef.Create("net.fakelagrand", 0f, CVar.CHEAT);

        /// <summary>
        /// Add random fake duplicates to all outgoing UDP network packets, as a ratio of how many packets to duplicate.
        /// 0 = no packets duplicated, 1 = all packets duplicated.
        /// </summary>
        public static readonly CVarDef<float> NetFakeDuplicates = CVarDef.Create("net.fakeduplicates", 0f, CVar.CHEAT);
#endif

        /*
         * METRICS
         */

        /// <summary>
        /// Whether to enable a prometheus metrics server.
        /// </summary>
        public static readonly CVarDef<bool> MetricsEnabled =
            CVarDef.Create("metrics.enabled", false, CVar.SERVERONLY);

        /// <summary>
        /// The IP address to host the metrics server on.
        /// </summary>
        public static readonly CVarDef<string> MetricsHost =
            CVarDef.Create("metrics.host", "localhost", CVar.SERVERONLY);

        /// <summary>
        /// The port to host the metrics server on.
        /// </summary>
        public static readonly CVarDef<int> MetricsPort =
            CVarDef.Create("metrics.port", 44880, CVar.SERVERONLY);

        /// <summary>
        /// Enable detailed runtime metrics. Empty to disable.
        /// </summary>
        /// <remarks>
        /// Runtime metrics are provided by https://github.com/djluck/prometheus-net.DotNetRuntime.
        /// Granularity of metrics can be further configured with related CVars.
        /// </remarks>
        public static readonly CVarDef<bool> MetricsRuntime =
            CVarDef.Create("metrics.runtime", true, CVar.SERVERONLY);

        /// <summary>
        /// Mode for runtime GC metrics. Empty to disable.
        /// </summary>
        /// <remarks>
        /// See the documentation for prometheus-net.DotNetRuntime for values and their metrics:
        /// https://github.com/djluck/prometheus-net.DotNetRuntime/blob/master/docs/metrics-exposed-5.0.md
        /// </remarks>
        public static readonly CVarDef<string> MetricsRuntimeGc =
            CVarDef.Create("metrics.runtime_gc", "Counters", CVar.SERVERONLY);

        /// <summary>
        /// Histogram buckets for GC and pause times. Comma-separated list of floats, in milliseconds.
        /// </summary>
        public static readonly CVarDef<string> MetricsRuntimeGcHistogram =
            CVarDef.Create("metrics.runtime_gc_histogram", "0.5,1.0,2.0,4.0,6.0,10.0,15.0,20.0", CVar.SERVERONLY);

        /// <summary>
        /// Mode for runtime lock contention metrics. Empty to disable.
        /// </summary>
        /// <remarks>
        /// See the documentation for prometheus-net.DotNetRuntime for values and their metrics:
        /// https://github.com/djluck/prometheus-net.DotNetRuntime/blob/master/docs/metrics-exposed-5.0.md
        /// </remarks>
        public static readonly CVarDef<string> MetricsRuntimeContention =
            CVarDef.Create("metrics.runtime_contention", "Counters", CVar.SERVERONLY);

        /// <summary>
        /// Sample lock contention every N events. Higher numbers increase accuracy but also memory use.
        /// </summary>
        public static readonly CVarDef<int> MetricsRuntimeContentionSampleRate =
            CVarDef.Create("metrics.runtime_contention_sample_rate", 50, CVar.SERVERONLY);

        /// <summary>
        /// Mode for runtime thread pool metrics. Empty to disable.
        /// </summary>
        /// <remarks>
        /// See the documentation for prometheus-net.DotNetRuntime for values and their metrics:
        /// https://github.com/djluck/prometheus-net.DotNetRuntime/blob/master/docs/metrics-exposed-5.0.md
        /// </remarks>
        public static readonly CVarDef<string> MetricsRuntimeThreadPool =
            CVarDef.Create("metrics.runtime_thread_pool", "Counters", CVar.SERVERONLY);

        /// <summary>
        /// Histogram buckets for thread pool queue length.
        /// </summary>
        public static readonly CVarDef<string> MetricsRuntimeThreadPoolQueueHistogram =
            CVarDef.Create("metrics.runtime_thread_pool_queue_histogram", "0,10,30,60,120,180", CVar.SERVERONLY);

        /// <summary>
        /// Mode for runtime JIT metrics. Empty to disable.
        /// </summary>
        /// <remarks>
        /// See the documentation for prometheus-net.DotNetRuntime for values and their metrics:
        /// https://github.com/djluck/prometheus-net.DotNetRuntime/blob/master/docs/metrics-exposed-5.0.md
        /// </remarks>
        public static readonly CVarDef<string> MetricsRuntimeJit =
            CVarDef.Create("metrics.runtime_jit", "Counters", CVar.SERVERONLY);

        /// <summary>
        /// Sample JIT every N events. Higher numbers increase accuracy but also memory use.
        /// </summary>
        public static readonly CVarDef<int> MetricsRuntimeJitSampleRate =
            CVarDef.Create("metrics.runtime_jit_sample_rate", 10, CVar.SERVERONLY);

        /// <summary>
        /// Mode for runtime exception metrics. Empty to disable.
        /// </summary>
        /// <remarks>
        /// See the documentation for prometheus-net.DotNetRuntime for values and their metrics:
        /// https://github.com/djluck/prometheus-net.DotNetRuntime/blob/master/docs/metrics-exposed-5.0.md
        /// </remarks>
        public static readonly CVarDef<string> MetricsRuntimeException =
            CVarDef.Create("metrics.runtime_exception", "Counters", CVar.SERVERONLY);

        /// <summary>
        /// Mode for runtime TCP socket metrics. Empty to disable.
        /// </summary>
        /// <remarks>
        /// See the documentation for prometheus-net.DotNetRuntime for values and their metrics:
        /// https://github.com/djluck/prometheus-net.DotNetRuntime/blob/master/docs/metrics-exposed-5.0.md
        /// </remarks>
        public static readonly CVarDef<string> MetricsRuntimeSocket =
            CVarDef.Create("metrics.runtime_socket", "Counters", CVar.SERVERONLY);

        /*
         * STATUS
         */

        /// <summary>
        /// Whether to enable the HTTP status API server.
        /// </summary>
        /// <remarks>
        /// This is necessary for people to be able to connect via the launcher.
        /// </remarks>
        public static readonly CVarDef<bool> StatusEnabled =
            CVarDef.Create("status.enabled", true, CVar.ARCHIVE | CVar.SERVERONLY);

        /// <summary>
        /// Prefix address to bind the HTTP status API server to.
        /// This is in the form of addr:port, with * serving as a wildcard "all".
        /// If empty (the default), this is automatically generated to match the UDP ports.
        /// </summary>
        public static readonly CVarDef<string> StatusBind =
            CVarDef.Create("status.bind", "", CVar.ARCHIVE | CVar.SERVERONLY);

        /// <summary>
        /// Max amount of concurrent connections to the HTTP status server.
        /// Note that this is for actively processing requests, not kept-alive/pooled connections.
        /// </summary>
        public static readonly CVarDef<int> StatusMaxConnections =
            CVarDef.Create("status.max_connections", 5, CVar.SERVERONLY);

        /// <summary>
        /// UDP address that should be advertised and the launcher will use to connect to.
        /// If not set, the launcher will automatically infer this based on the address it already has.
        /// </summary>
        public static readonly CVarDef<string> StatusConnectAddress =
            CVarDef.Create("status.connectaddress", "", CVar.ARCHIVE | CVar.SERVERONLY);

        /*
         * BUILD
         */

        /// <summary>
        /// Engine version that launcher needs to connect to this server.
        /// </summary>
        public static readonly CVarDef<string> BuildEngineVersion =
            CVarDef.Create("build.engine_version", "", CVar.SERVERONLY);

        /// <summary>
        /// Fork ID, as a hint to the launcher to manage local files.
        /// This can be anything, it does not need a strict format.
        /// </summary>
        public static readonly CVarDef<string> BuildForkId =
            CVarDef.Create("build.fork_id", "", CVar.SERVERONLY);

        /// <summary>
        /// Version string, as a hint to the launcher to manage local files.
        /// This can be anything, it does not need a strict format.
        /// </summary>
        public static readonly CVarDef<string> BuildVersion =
            CVarDef.Create("build.version", "", CVar.SERVERONLY);

        /// <summary>
        /// Content pack the launcher should download to connect to this server.
        /// </summary>
        public static readonly CVarDef<string> BuildDownloadUrl =
            CVarDef.Create("build.download_url", string.Empty, CVar.SERVERONLY);

        /// <summary>
        /// SHA-256 hash of the content pack hosted at <c>build.download_url</c>
        /// </summary>
        public static readonly CVarDef<string> BuildHash =
            CVarDef.Create("build.hash", "", CVar.SERVERONLY);

        /*
         * WATCHDOG
         */

        /// <summary>
        /// API token set by the watchdog to communicate to the server.
        /// </summary>
        public static readonly CVarDef<string> WatchdogToken =
            CVarDef.Create("watchdog.token", "", CVar.SERVERONLY);

        /// <summary>
        /// Watchdog server identifier for this server.
        /// </summary>
        public static readonly CVarDef<string> WatchdogKey =
            CVarDef.Create("watchdog.key", "", CVar.SERVERONLY);

        /// <summary>
        /// Base URL of the watchdog on the local machine.
        /// </summary>
        public static readonly CVarDef<string> WatchdogBaseUrl =
            CVarDef.Create("watchdog.baseUrl", "http://localhost:5000", CVar.SERVERONLY);

        /*
         * GAME
         */

        /// <summary>
        /// Hard max-cap of concurrent connections for the main game networking.
        /// </summary>
        /// <remarks>
        /// This cannot be bypassed in any way, since it is used by Lidgren internally.
        /// </remarks>
        public static readonly CVarDef<int> GameMaxPlayers =
            CVarDef.Create("game.maxplayers", 32, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// Name of the game server. This shows up in the launcher and potentially parts of the UI.
        /// </summary>
        public static readonly CVarDef<string> GameHostName =
            CVarDef.Create("game.hostname", "MyServer", CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// If a grid is shrunk to include no more tiles should it be deleted.
        /// </summary>
        public static readonly CVarDef<bool> GameDeleteEmptyGrids =
            CVarDef.Create("game.delete_empty_grids", true, CVar.ARCHIVE | CVar.SERVER);

        /// <summary>
        /// Automatically pause simulation if there are no players connected to the game server.
        /// </summary>
        public static readonly CVarDef<bool> GameAutoPauseEmpty =
            CVarDef.Create("game.auto_pause_empty", true, CVar.SERVERONLY);

        /*
         * LOG
         */

        /// <summary>
        /// Write server log to disk.
        /// </summary>
        public static readonly CVarDef<bool> LogEnabled =
            CVarDef.Create("log.enabled", true, CVar.ARCHIVE | CVar.SERVERONLY);

        /// <summary>
        /// Path to put log files in if log writing is enabled.
        /// </summary>
        public static readonly CVarDef<string> LogPath =
            CVarDef.Create("log.path", "logs", CVar.ARCHIVE | CVar.SERVERONLY);

        /// <summary>
        /// Format for individual log files, based on current date and time replacement.
        /// </summary>
        public static readonly CVarDef<string> LogFormat =
            CVarDef.Create("log.format", "log_%(date)s-T%(time)s.txt", CVar.ARCHIVE | CVar.SERVERONLY);

        /// <summary>
        /// Minimum log level for all server logging.
        /// </summary>
        public static readonly CVarDef<LogLevel> LogLevel =
            CVarDef.Create("log.level", Log.LogLevel.Info, CVar.ARCHIVE | CVar.SERVERONLY);

        /// <summary>
        /// Log a separate exception log for all exceptions that occur.
        /// </summary>
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

        /// <summary>
        /// Whether to send the server log to Grafana Loki.
        /// </summary>
        public static readonly CVarDef<bool> LokiEnabled =
            CVarDef.Create("loki.enabled", false, CVar.SERVERONLY);

        /// <summary>
        /// The name of the current server, set as the value of the "Server" label in Loki.
        /// </summary>
        public static readonly CVarDef<string> LokiName =
            CVarDef.Create("loki.name", "", CVar.SERVERONLY);

        /// <summary>
        /// The address of the Loki server to send to.
        /// </summary>
        public static readonly CVarDef<string> LokiAddress =
            CVarDef.Create("loki.address", "", CVar.SERVERONLY);

        /// <summary>
        /// If set, a HTTP Basic auth username to use when talking to Loki.
        /// </summary>
        public static readonly CVarDef<string> LokiUsername =
            CVarDef.Create("loki.username", "", CVar.SERVERONLY);

        /// <summary>
        /// If set, a HTTP Basic auth password to use when talking to Loki.
        /// </summary>
        public static readonly CVarDef<string> LokiPassword =
            CVarDef.Create("loki.password", "", CVar.SERVERONLY);

        /*
         * AUTH
         */

        /// <summary>
        /// Mode with which to handle authentication on the server.
        /// See the documentation of the <see cref="Network.AuthMode"/> enum for values.
        /// </summary>
        public static readonly CVarDef<int> AuthMode =
            CVarDef.Create("auth.mode", (int) Network.AuthMode.Optional, CVar.SERVERONLY);

        /// <summary>
        /// Allow unauthenticated localhost connections, even if the auth mode is set to required.
        /// These connections have a "localhost@" prefix as username.
        /// </summary>
        public static readonly CVarDef<bool> AuthAllowLocal =
            CVarDef.Create("auth.allowlocal", true, CVar.SERVERONLY);

        // Only respected on server, client goes through IAuthManager for security.
        /// <summary>
        /// Authentication server address.
        /// </summary>
        public static readonly CVarDef<string> AuthServer =
            CVarDef.Create("auth.server", AuthManager.DefaultAuthServer, CVar.SERVERONLY);

        /*
         * DISPLAY
         */

        /// <summary>
        /// Enable VSync for rendering.
        /// </summary>
        public static readonly CVarDef<bool> DisplayVSync =
            CVarDef.Create("display.vsync", true, CVar.ARCHIVE | CVar.CLIENTONLY);

        /// <summary>
        /// Window mode for the main game window. 0 = windowed, 1 = fullscreen.
        /// </summary>
        public static readonly CVarDef<int> DisplayWindowMode =
            CVarDef.Create("display.windowmode", 0, CVar.ARCHIVE | CVar.CLIENTONLY);

        /// <summary>
        /// Initial width of the game window when running on windowed mode.
        /// </summary>
        public static readonly CVarDef<int> DisplayWidth =
            CVarDef.Create("display.width", 1280, CVar.CLIENTONLY);

        /// <summary>
        /// Initial height of the game window when running on windowed mode.
        /// </summary>
        public static readonly CVarDef<int> DisplayHeight =
            CVarDef.Create("display.height", 720, CVar.CLIENTONLY);

        /// <summary>
        /// Factor by which to divide the horizontal and vertical resolution of lighting framebuffers,
        /// relative to the viewport framebuffer size.
        /// </summary>
        public static readonly CVarDef<int> DisplayLightMapDivider =
            CVarDef.Create("display.lightmapdivider", 2, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        /// Maximum amount of lights that can be rendered in a single viewport at once.
        /// </summary>
        public static readonly CVarDef<int> DisplayMaxLightsPerScene =
            CVarDef.Create("display.maxlightsperscene", 128, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        /// Whether to give shadows a soft edge when rendering.
        /// </summary>
        public static readonly CVarDef<bool> DisplaySoftShadows =
            CVarDef.Create("display.softshadows", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        /// Apply a gaussian blur to the final lighting framebuffer to smoothen it out a little.
        /// </summary>
        public static readonly CVarDef<bool> DisplayBlurLight =
            CVarDef.Create("display.blur_light", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        /// Factor by which to blur the lighting framebuffer under <c>display.blur_light</c>.
        /// </summary>
        public static readonly CVarDef<float> DisplayBlurLightFactor =
            CVarDef.Create("display.blur_light_factor", 0.001f, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        /// UI scale for all UI controls. If zero, this value is automatically calculated from the OS.
        /// </summary>
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
        /// This can change certain behaviors like GL version selection to try to avoid driver crashes/bugs.
        /// </remarks>
        public static readonly CVarDef<bool> DisplayCompat =
            CVarDef.Create("display.compat", false, CVar.CLIENTONLY);

        /// <summary>
        /// Which OpenGL version to use for the OpenGL renderer.
        /// Values correspond to the (private) RendererOpenGLVersion enum in Clyde.
        /// </summary>
        public static readonly CVarDef<int> DisplayOpenGLVersion =
            CVarDef.Create("display.opengl_version", 0, CVar.CLIENTONLY);

        /// <summary>
        /// On Windows, use ANGLE as OpenGL implementation.
        /// </summary>
        public static readonly CVarDef<bool> DisplayAngle =
            CVarDef.Create("display.angle", false, CVar.CLIENTONLY);

        /// <summary>
        /// Use a custom DXGI swap chain when using ANGLE.
        /// Should improve performance and fixes main window sRGB handling with ANGLE.
        /// </summary>
        public static readonly CVarDef<bool> DisplayAngleCustomSwapChain =
            CVarDef.Create("display.angle_custom_swap_chain", true, CVar.CLIENTONLY);

        /// <summary>
        /// Force ANGLE to create a GLES2 context (not a compatible GLES3 context).
        /// </summary>
        public static readonly CVarDef<bool> DisplayAngleForceEs2 =
            CVarDef.Create("display.angle_force_es2", false, CVar.CLIENTONLY);

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
        /// What type of GPU to prefer when creating a graphics context, for things such as hybrid GPU laptops.
        /// </summary>
        /// <remarks>
        /// This setting is not always respect depending on platform and rendering API used.
        /// Values are:
        /// 0 = unspecified (DXGI_GPU_PREFERENCE_UNSPECIFIED)
        /// 1 = minimum power (DXGI_GPU_PREFERENCE_MINIMUM_POWER)
        /// 2 = high performance (DXGI_GPU_PREFERENCE_HIGH_PERFORMANCE)
        /// </remarks>
        public static readonly CVarDef<int> DisplayGpuPreference =
            CVarDef.Create("display.gpu_preference", 2, CVar.CLIENTONLY);

        /// <summary>
        /// Use EGL to create GL context instead of GLFW, if possible.
        /// </summary>
        /// <remarks>
        /// This only tries to use EGL if on a platform like X11 or Windows (w/ ANGLE) where it is possible.
        /// </remarks>
        public static readonly CVarDef<bool> DisplayEgl =
            CVarDef.Create("display.egl", false, CVar.CLIENTONLY);

        /// <summary>
        /// Base DPI to render fonts at. This can be further scaled based on <c>display.uiScale</c>.
        /// </summary>
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

        /// <summary>
        /// Buffer size of input command channel from windowing thread to main game thread.
        /// </summary>
        public static readonly CVarDef<int> DisplayInputBufferSize =
            CVarDef.Create("display.input_buffer_size", 32, CVar.CLIENTONLY);

        /// <summary>
        /// Insert stupid performance hitches into the windowing thread, to test how the game thread handles it.
        /// </summary>
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

        /// <summary>
        /// Use US QWERTY hotkeys for reported key names.
        /// </summary>
        public static readonly CVarDef<bool> DisplayUSQWERTYHotkeys =
            CVarDef.Create("display.use_US_QWERTY_hotkeys", false, CVar.CLIENTONLY | CVar.ARCHIVE);

        /*
         * AUDIO
         */

        public static readonly CVarDef<int> AudioAttenuation =
            CVarDef.Create("audio.attenuation", (int) Attenuation.Default, CVar.REPLICATED | CVar.ARCHIVE);

        /// <summary>
        /// Audio device to try to output audio to by default.
        /// </summary>
        public static readonly CVarDef<string> AudioDevice =
            CVarDef.Create("audio.device", string.Empty, CVar.CLIENTONLY);

        /// <summary>
        /// Master volume for audio output.
        /// </summary>
        public static readonly CVarDef<float> AudioMasterVolume =
            CVarDef.Create("audio.mastervolume", 1.0f, CVar.ARCHIVE | CVar.CLIENTONLY);

        /*
         * PLAYER
         */

        /// <summary>
        /// Player name to send from user, if not overriden by a myriad of factors.
        /// </summary>
        public static readonly CVarDef<string> PlayerName =
            CVarDef.Create("player.name", "JoeGenero", CVar.ARCHIVE | CVar.CLIENTONLY);

        /*
         * PHYSICS
         */

        /// <summary>
        /// How much to expand broadphase checking for. This is useful for cross-grid collisions.
        /// Performance impact if additional broadphases are being checked.
        /// </summary>
        public static readonly CVarDef<float> BroadphaseExpand =
            CVarDef.Create("physics.broadphase_expand", 2f, CVar.ARCHIVE | CVar.REPLICATED);

        // Grid fixtures
        /// <summary>
        /// I'ma be real with you: the only reason this exists is to get tests working.
        /// </summary>
        public static readonly CVarDef<bool> GenerateGridFixtures =
            CVarDef.Create("physics.grid_fixtures", true, CVar.REPLICATED);

        /// <summary>
        /// How much to enlarge grids when determining their fixture bounds.
        /// </summary>
        public static readonly CVarDef<float> GridFixtureEnlargement =
            CVarDef.Create("physics.grid_fixture_enlargement", -PhysicsConstants.PolygonRadius, CVar.ARCHIVE | CVar.REPLICATED);

        // - Contacts
        public static readonly CVarDef<int> ContactMultithreadThreshold =
            CVarDef.Create("physics.contact_multithread_threshold", 32);

        public static readonly CVarDef<int> ContactMinimumThreads =
            CVarDef.Create("physics.contact_minimum_threads", 2);

        // - Sleep
        public static readonly CVarDef<float> AngularSleepTolerance =
            CVarDef.Create("physics.angsleeptol", 0.3f / 180.0f * MathF.PI);

        public static readonly CVarDef<float> LinearSleepTolerance =
            CVarDef.Create("physics.linsleeptol", 0.1f);

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

        /// <summary>
        /// Enable Discord rich presence integration.
        /// </summary>
        public static readonly CVarDef<bool> DiscordEnabled =
            CVarDef.Create("discord.enabled", true, CVar.CLIENTONLY);

        /*
         * RES
         */

        /// <summary>
        /// Verify that resource path capitalization is correct, even on case-insensitive file systems such as Windows.
        /// </summary>
        public static readonly CVarDef<bool> ResCheckPathCasing =
            CVarDef.Create("res.checkpathcasing", false);

        /// <summary>
        /// Preload all textures at client startup to avoid hitches at runtime.
        /// </summary>
        public static readonly CVarDef<bool> ResTexturePreloadingEnabled =
            CVarDef.Create("res.texturepreloadingenabled", true, CVar.CLIENTONLY);

        // TODO: Currently unimplemented.
        /// <summary>
        /// Cache texture preload data to speed things up even further.
        /// </summary>
        public static readonly CVarDef<bool> ResTexturePreloadCache =
            CVarDef.Create("res.texture_preload_cache", true, CVar.CLIENTONLY);

        /// <summary>
        /// Override seekability of resource streams returned by ResourceManager.
        /// See <see cref="ContentPack.StreamSeekMode"/> for int values.
        /// </summary>
        /// <remarks>
        /// This is intended to be a debugging tool primarily.
        /// Non-default seek modes WILL result in worse performance.
        /// </remarks>
        public static readonly CVarDef<int> ResStreamSeekMode =
            CVarDef.Create("res.stream_seek_mode", (int)ContentPack.StreamSeekMode.None);

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

        /*
         * HUB
         * CVars related to public master server hub
         */

        /// <summary>
        /// Whether to advertise this server to the public server hub.
        /// </summary>
        public static readonly CVarDef<bool> HubAdvertise =
            CVarDef.Create("hub.advertise", false, CVar.SERVERONLY);

        /// <summary>
        /// URL of the master hub server to advertise to.
        /// </summary>
        public static readonly CVarDef<string> HubMasterUrl =
            CVarDef.Create("hub.master_url", "https://central.spacestation14.io/hub/", CVar.SERVERONLY);

        /// <summary>
        /// URL of this server to advertise.
        /// This is automatically inferred by the hub server based on IP address if left empty,
        /// but if you want to specify a domain or use <c>ss14://</c> you should specify this manually.
        /// You also have to set this if you change status.bind.
        /// </summary>
        public static readonly CVarDef<string> HubServerUrl =
            CVarDef.Create("hub.server_url", "", CVar.SERVERONLY);

        /// <summary>
        /// URL to use to automatically try to detect IPv4 address.
        /// This is only used if hub.server_url is unset.
        /// </summary>
        public static readonly CVarDef<string> HubIpifyUrl =
            CVarDef.Create("hub.ipify_url", "https://api.ipify.org?format=json", CVar.SERVERONLY);

        /// <summary>
        /// How long to wait between advertise pings to the hub server.
        /// </summary>
        public static readonly CVarDef<int> HubAdvertiseInterval =
            CVarDef.Create("hub.advertise_interval", 120, CVar.SERVERONLY);
    }
}
