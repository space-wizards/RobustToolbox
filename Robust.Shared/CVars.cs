using System;
using System.Threading;
using Lidgren.Network;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Physics;

namespace Robust.Shared
{
    /// <seealso cref="CVarDefaultOverrides"/>
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
        /// Hard max-cap of concurrent connections for the main game networking.
        /// </summary>
        /// <remarks>
        /// This cannot be bypassed in any way, since it is used by Lidgren internally.
        /// </remarks>
        public static readonly CVarDef<int> NetMaxConnections =
            CVarDef.Create("net.max_connections", 256, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

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
        /// Size of the pool for Lidgren's array buffers to send messages.
        /// Set to 0 to disable pooling; max is 8192.
        /// </summary>
        /// <remarks>
        /// Higher just means more potentially wasted space and slower pool retrieval.
        /// </remarks>
        public static readonly CVarDef<int> NetPoolSize =
            CVarDef.Create("net.pool_size", 512, CVar.CLIENT | CVar.SERVER);

        /// <summary>
        /// Maximum UDP payload size to send by default, for IPv4.
        /// </summary>
        /// <seealso cref="NetMtuExpand"/>
        /// <seealso cref="NetMtuIpv6"/>
        public static readonly CVarDef<int> NetMtu =
            CVarDef.Create("net.mtu", 700, CVar.ARCHIVE);

        /// <summary>
        /// Maximum UDP payload size to send by default, for IPv6.
        /// </summary>
        /// <seealso cref="NetMtu"/>
        /// <seealso cref="NetMtuExpand"/>
        public static readonly CVarDef<int> NetMtuIpv6 =
            CVarDef.Create("net.mtu_ipv6", NetPeerConfiguration.kDefaultMTUV6, CVar.ARCHIVE);

        /// <summary>
        /// If set, automatically try to detect MTU above <see cref="NetMtu"/>.
        /// </summary>
        /// <seealso cref="NetMtu"/>
        /// <seealso cref="NetMtuIpv6"/>
        /// <seealso cref="NetMtuExpandFrequency"/>
        /// <seealso cref="NetMtuExpandFailAttempts"/>
        public static readonly CVarDef<bool> NetMtuExpand =
            CVarDef.Create("net.mtu_expand", false, CVar.ARCHIVE);

        /// <summary>
        /// Interval between MTU expansion attempts, in seconds.
        /// </summary>
        /// <remarks>
        /// This property is named incorrectly: it is actually an interval, not a frequency.
        /// The name is chosen to match Lidgren's <see cref="NetPeerConfiguration.ExpandMTUFrequency"/>.
        /// </remarks>
        /// <seealso cref="NetMtuExpand"/>
        public static readonly CVarDef<float> NetMtuExpandFrequency =
            CVarDef.Create("net.mtu_expand_frequency", 2f, CVar.ARCHIVE);

        /// <summary>
        /// How many times an MTU expansion attempt can fail before settling on a final MTU value.
        /// </summary>
        /// <seealso cref="NetMtuExpand"/>
        public static readonly CVarDef<int> NetMtuExpandFailAttempts =
            CVarDef.Create("net.mtu_expand_fail_attempts", 5, CVar.ARCHIVE);

        /// <summary>
        /// Whether to enable verbose debug logging in Lidgren.
        /// </summary>
        /// <seealso cref="NetMtuExpand"/>
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
            CVarDef.Create("net.interp", true, CVar.ARCHIVE | CVar.CLIENTONLY);

        /// <summary>
        /// The target number of game states to keep buffered up to smooth out network inconsistency.
        /// </summary>
        public static readonly CVarDef<int> NetBufferSize =
            CVarDef.Create("net.buffer_size", 2, CVar.ARCHIVE | CVar.CLIENTONLY);

        /// <summary>
        /// The maximum size of the game state buffer. If this is exceeded the client will request a full game state.
        /// Values less than <see cref="GameStateProcessor.MinimumMaxBufferSize"/> will be ignored.
        /// </summary>
        public static readonly CVarDef<int> NetMaxBufferSize =
            CVarDef.Create("net.max_buffer_size", 512, CVar.ARCHIVE | CVar.CLIENTONLY);

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
            CVarDef.Create("net.predict", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        /// Extra amount of ticks to run-ahead for prediction on the client.
        /// </summary>
        public static readonly CVarDef<int> NetPredictTickBias =
            CVarDef.Create("net.predict_tick_bias", 1, CVar.CLIENTONLY | CVar.ARCHIVE);

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
                CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<int> NetStateBufMergeThreshold =
            CVarDef.Create("net.state_buf_merge_threshold", 5, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        /// Whether to cull entities sent to clients from the server.
        /// If this is on, only entities immediately close to a client will be sent.
        /// Otherwise, all entities will be sent to all clients.
        /// </summary>
        public static readonly CVarDef<bool> NetPVS =
            CVarDef.Create("net.pvs", true, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// Size increments for the automatic growth of Pvs' entity data storage. 0 will increase it by factors of 2
        /// </summary>
        public static readonly CVarDef<int> NetPvsEntityGrowth =
            CVarDef.Create("net.pvs_entity_growth", 1 << 16, CVar.ARCHIVE | CVar.SERVERONLY);

        /// <summary>
        /// Initial size of PVS' entity data storage.
        /// </summary>
        public static readonly CVarDef<int> NetPvsEntityInitial =
            CVarDef.Create("net.pvs_entity_initial", 1 << 16, CVar.ARCHIVE | CVar.SERVERONLY);

        /// <summary>
        /// Maximum ever size of PVS' entity data storage.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Arbitrarily set to a default of 16 million entities.
        /// Increasing this parameter does not increase real memory usage, only virtual.
        /// </para>
        /// </remarks>
        public static readonly CVarDef<int> NetPvsEntityMax =
            CVarDef.Create("net.pvs_entity_max", 1 << 24, CVar.ARCHIVE | CVar.SERVERONLY);

        /// <summary>
        /// If false, this will run more parts of PVS synchronously. This will generally slow it down, can be useful
        /// for collecting tick timing metrics.
        /// </summary>
        public static readonly CVarDef<bool> NetPvsAsync =
            CVarDef.Create("net.pvs_async", true, CVar.ARCHIVE | CVar.SERVERONLY);

        /// <summary>
        /// View size to take for PVS calculations, as the size of the sides of a square centered on the view points of
        /// clients. See also <see cref="NetPvsPriorityRange"/>.
        /// </summary>
        public static readonly CVarDef<float> NetMaxUpdateRange =
            CVarDef.Create("net.pvs_range", 25f, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// A variant of <see cref="NetMaxUpdateRange"/> that is used to limit the view-distance of entities with the
        /// <see cref="MetaDataFlags.PvsPriority"/> flag set. This can be used to extend the range at which certain
        /// entities become visible.
        /// </summary>
        /// <remarks>
        /// This is useful for entities like lights and occluders to try and prevent noticeable pop-in as players
        /// move around. Note that this has no effect if it is less than <see cref="NetMaxUpdateRange"/>, and that this
        /// only works for entities that are directly parented to a grid or map.
        /// </remarks>
        public static readonly CVarDef<float> NetPvsPriorityRange =
            CVarDef.Create("net.pvs_priority_range", 32.5f, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// Maximum allowed delay between the current tick and a client's last acknowledged tick before we send the
        /// next game state reliably and simply force update the acked tick,
        /// </summary>
        public static readonly CVarDef<int> NetForceAckThreshold =
            CVarDef.Create("net.force_ack_threshold", 60, CVar.ARCHIVE | CVar.SERVERONLY);

        /// <summary>
        /// This limits the number of new entities that can be sent to a client in a single game state. This exists to
        /// avoid stuttering on the client when it has to spawn a bunch of entities in a single tick. If ever entity
        /// spawning isn't hot garbage, this can be increased.
        /// </summary>
        public static readonly CVarDef<int> NetPVSEntityBudget =
            CVarDef.Create("net.pvs_budget", 50, CVar.ARCHIVE | CVar.REPLICATED | CVar.CLIENT);

        /// <summary>
        /// This limits the number of entities that can re-enter a client's view in a single game state. This exists to
        /// avoid stuttering on the client when it has to update the transform of a bunch (700+) of entities in a single
        /// tick. Ideally this would just be handled client-side somehow.
        /// </summary>
        public static readonly CVarDef<int> NetPVSEntityEnterBudget =
            CVarDef.Create("net.pvs_enter_budget", 200, CVar.ARCHIVE | CVar.REPLICATED | CVar.CLIENT);

        /// <summary>
        /// The amount of pvs-exiting entities that a client will process in a single tick.
        /// </summary>
        public static readonly CVarDef<int> NetPVSEntityExitBudget =
            CVarDef.Create("net.pvs_exit_budget", 75, CVar.ARCHIVE | CVar.CLIENTONLY);

        /// <summary>
        /// ZSTD compression level to use when compressing game states. Used by both networking and replays.
        /// </summary>
        public static readonly CVarDef<int> NetPvsCompressLevel =
            CVarDef.Create("net.pvs_compress_level", 3, CVar.ARCHIVE);

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
            CVarDef.Create("net.tickrate", 30, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// Offset CurTime at server start by this amount (in seconds).
        /// </summary>
        public static readonly CVarDef<int> NetTimeStartOffset =
            CVarDef.Create("net.time_start_offset", 0, CVar.SERVERONLY);

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

        /// <summary>
        /// When using Happy Eyeballs to try both IPv6 over IPv4, the delay that IPv4 gets to get less priority.
        /// </summary>
        public static readonly CVarDef<float> NetHappyEyeballsDelay =
            CVarDef.Create("net.happy_eyeballs_delay", 0.025f, CVar.CLIENTONLY);

        /// <summary>
        /// Controls whether the networking library will log warning messages.
        /// </summary>
        /// <remarks>
        /// Disabling this should make the networking layer more resilient against some DDoS attacks.
        /// </remarks>
        public static readonly CVarDef<bool> NetLidgrenLogWarning =
            CVarDef.Create("net.lidgren_log_warning", true);

        /// <summary>
        /// Controls whether the networking library will log error messages.
        /// </summary>
        public static readonly CVarDef<bool> NetLidgrenLogError =
            CVarDef.Create("net.lidgren_log_error", true);

        /// <summary>
        /// If true, run network message encryption on another thread.
        /// </summary>
        public static readonly CVarDef<bool> NetEncryptionThread =
            CVarDef.Create("net.encryption_thread", true);

        /// <summary>
        /// Outstanding buffer size used by <see cref="NetEncryptionThread"/>.
        /// </summary>
        public static readonly CVarDef<int> NetEncryptionThreadChannelSize =
            CVarDef.Create("net.encryption_thread_channel_size", 16);

        /// <summary>
        /// Whether the server should request HWID system for client identification.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Note that modern HWIDs are only available if the connection is authenticated.
        /// </para>
        /// </remarks>
        public static readonly CVarDef<bool> NetHWId =
            CVarDef.Create("net.hwid", true, CVar.SERVERONLY);


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

        /// <summary>
        /// Whether to run a <see cref="GC.Collect()"/> operation after the engine is finished initializing.
        /// </summary>
        public static readonly CVarDef<bool> SysGCCollectStart =
            CVarDef.Create("sys.gc_collect_start", true);

        /// <summary>
        /// Use precise sleeping methods in the game loop.
        /// </summary>
        public static readonly CVarDef<bool> SysPreciseSleep =
            CVarDef.Create("sys.precise_sleep", true);

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
        /// Sets a fixed interval (seconds) for internal collection of certain metrics,
        /// when not using the Prometheus metrics server.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Most metrics are internally implemented directly via the prometheus-net library.
        /// These metrics can only be scraped by the Prometheus metrics server (<see cref="MetricsEnabled"/>).
        /// However, newer metrics are implemented with the <c>System.Diagnostics.Metrics</c> library in the .NET runtime.
        /// These metrics can be scraped through more means, such as <c>dotnet counters</c>.
        /// </para>
        /// <para>
        /// While many metrics are simple counters that can "just" be reported,
        /// some metrics require more advanced internal work and need some code to be ran internally
        /// before their values are made current. When collecting metrics via a
        /// method other than the Prometheus metrics server, these metrics pose a problem,
        /// as there is no way for the game to update them before collection properly.
        /// </para>
        /// <para>
        /// This CVar acts as a fallback: if set to a value other than 0 (disabled),
        /// these metrics will be internally updated at the interval provided.
        /// </para>
        /// <para>
        /// This does not need to be enabled if metrics are collected exclusively via the Prometheus metrics server.
        /// </para>
        /// </remarks>
        public static readonly CVarDef<float> MetricsUpdateInterval =
            CVarDef.Create("metrics.update_interval", 0f, CVar.SERVERONLY);

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

        /// <summary>
        /// HTTP(S) link to a privacy policy that the user must accept to connect to the server.
        /// </summary>
        /// <remarks>
        /// This must be set along with <see cref="StatusPrivacyPolicyIdentifier"/> and
        /// <see cref="StatusPrivacyPolicyVersion"/> for the user to be prompted about a privacy policy.
        /// </remarks>
        public static readonly CVarDef<string> StatusPrivacyPolicyLink =
            CVarDef.Create("status.privacy_policy_link", "https://example.com/privacy", CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// An identifier for privacy policy specified by <see cref="StatusPrivacyPolicyLink"/>.
        /// This must be globally unique.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This value must be globally unique per server community. Servers that want to enforce a
        /// privacy policy should set this to a value that is unique to their server and, preferably, recognizable.
        /// </para>
        /// <para>
        /// This value is stored by the launcher to keep track of what privacy policies a player has accepted.
        /// </para>
        /// </remarks>
        public static readonly CVarDef<string> StatusPrivacyPolicyIdentifier =
            CVarDef.Create("status.privacy_policy_identifier", "", CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        /// A "version" for the privacy policy specified by <see cref="StatusPrivacyPolicyLink"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This parameter is stored by the launcher and should be modified whenever your server's privacy policy changes.
        /// </para>
        /// </remarks>
        public static readonly CVarDef<string> StatusPrivacyPolicyVersion =
            CVarDef.Create("status.privacy_policy_version", "", CVar.SERVER | CVar.REPLICATED);

        /*
         * BUILD
         */

        /// <summary>
        /// Engine version that launcher needs to connect to this server.
        /// </summary>
        public static readonly CVarDef<string> BuildEngineVersion =
            CVarDef.Create("build.engine_version",
                typeof(CVars).Assembly.GetName().Version?.ToString(3) ?? String.Empty);

        /// <summary>
        /// Fork ID, as a hint to the launcher to manage local files.
        /// This can be anything, it does not need a strict format.
        /// </summary>
        public static readonly CVarDef<string> BuildForkId =
            CVarDef.Create("build.fork_id", "");

        /// <summary>
        /// Version string, as a hint to the launcher to manage local files.
        /// This can be anything, it does not need a strict format.
        /// </summary>
        public static readonly CVarDef<string> BuildVersion =
            CVarDef.Create("build.version", "");

        /// <summary>
        /// Content pack the launcher should download to connect to this server.
        /// </summary>
        public static readonly CVarDef<string> BuildDownloadUrl =
            CVarDef.Create("build.download_url", string.Empty);

        /// <summary>
        /// URL of the content manifest the launcher should download to connect to this server.
        /// </summary>
        public static readonly CVarDef<string> BuildManifestUrl =
            CVarDef.Create("build.manifest_url", string.Empty);

        /// <summary>
        /// URL at which the launcher can download the manifest game files.
        /// </summary>
        public static readonly CVarDef<string> BuildManifestDownloadUrl =
            CVarDef.Create("build.manifest_download_url", string.Empty);

        /// <summary>
        /// SHA-256 hash of the content pack hosted at <c>build.download_url</c>
        /// </summary>
        public static readonly CVarDef<string> BuildHash =
            CVarDef.Create("build.hash", "");

        /// <summary>
        /// SHA-256 hash of the manifest hosted at <c>build.manifest_url</c>
        /// </summary>
        public static readonly CVarDef<string> BuildManifestHash =
            CVarDef.Create("build.manifest_hash", "");

        /// <summary>
        /// Allows you to disable the display of all entities in the spawn menu that are not labeled with the ShowSpawnMenu category.
        /// This is useful for forks that just want to disable the standard upstream content
        /// </summary>
        public static readonly CVarDef<string> EntitiesCategoryFilter =
            CVarDef.Create("build.entities_category_filter", "");

        /*
         * WATCHDOG
         */

        /// <summary>
        /// API token set by the watchdog to communicate to the server.
        /// </summary>
        public static readonly CVarDef<string> WatchdogToken =
            CVarDef.Create("watchdog.token", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

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
        [Obsolete("Use net.max_connections instead")]
        public static readonly CVarDef<int> GameMaxPlayers =
            CVarDef.Create("game.maxplayers", 0, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// Name of the game server. This shows up in the launcher and potentially parts of the UI.
        /// </summary>
        public static readonly CVarDef<string> GameHostName =
            CVarDef.Create("game.hostname", "MyServer", CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// Description of the game server in the launcher.
        /// </summary>
        public static readonly CVarDef<string> GameDesc =
            CVarDef.Create("game.desc", "Just another server, don't mind me!", CVar.SERVERONLY);

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
            CVarDef.Create("light.max_radius", 32.1f, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        /// Maximum number of lights that the client will draw.
        /// </summary>
        public static readonly CVarDef<int> MaxLightCount =
            CVarDef.Create("light.max_light_count", 2048, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        /// Maximum number of occluders that the client will draw. Values below 1024 have no effect.
        /// </summary>
        public static readonly CVarDef<int> MaxOccluderCount =
            CVarDef.Create("light.max_occluder_count", 2048, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        /// Scale used to modify the horizontal and vertical resolution of lighting framebuffers,
        /// relative to the viewport framebuffer size.
        /// </summary>
        public static readonly CVarDef<float> LightResolutionScale =
            CVarDef.Create("light.resolution_scale", 0.5f, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        /// Maximum amount of shadow-casting lights that can be rendered in a single viewport at once.
        /// </summary>
        public static readonly CVarDef<int> MaxShadowcastingLights =
            CVarDef.Create("light.max_shadowcasting_lights", 128, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        /// Whether to give shadows a soft edge when rendering.
        /// </summary>
        public static readonly CVarDef<bool> LightSoftShadows =
            CVarDef.Create("light.soft_shadows", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        /// Apply a gaussian blur to the final lighting framebuffer to smoothen it out a little.
        /// </summary>
        public static readonly CVarDef<bool> LightBlur=
            CVarDef.Create("light.blur", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        /// Factor by which to blur the lighting framebuffer under <c>light.blur</c>.
        /// </summary>
        public static readonly CVarDef<float> LightBlurFactor =
            CVarDef.Create("light.blur_factor", 0.001f, CVar.CLIENTONLY | CVar.ARCHIVE);

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
            CVarDef.Create("auth.mode", (int) Network.AuthMode.Required, CVar.SERVERONLY);

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
         * RENDERING
         */

        /// <summary>
        ///     This biases the RSI-direction used to draw diagonally oriented 4-directional sprites to avoid flickering between directions. A positive
        ///     value biases towards facing N/S, while a negative value will bias towards E/W.
        /// </summary>
        /// <remarks>
        ///     The bias needs to be large enough to prevent sprites on rotating grids from flickering, but should be
        ///     small enough that it is generally unnoticeable. Currently it is somewhat large to combat issues with
        ///     eye-lerping & grid rotations.
        /// </remarks>
        public static readonly CVarDef<double> RenderSpriteDirectionBias =
            CVarDef.Create("render.sprite_direction_bias", -0.05, CVar.ARCHIVE | CVar.CLIENTONLY);

        public static readonly CVarDef<string> RenderFOVColor =
            CVarDef.Create("render.fov_color", Color.Black.ToHex(), CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// Whether to render tile edges, which is where some tiles can partially overlap other adjacent tiles on a grid.
        /// E.g., snow tiles partly extending beyond their own tile to blend together with different adjacent tiles types.
        /// </summary>
        public static readonly CVarDef<bool> RenderTileEdges =
            CVarDef.Create("render.tile_edges", true, CVar.CLIENTONLY);

        /*
         *  CONTROLS
         */

        /// <summary>
        /// Milliseconds to wait to consider double-click delays.
        /// </summary>
        public static readonly CVarDef<int> DoubleClickDelay =
            CVarDef.Create("controls.double_click_delay", 250, CVar.ARCHIVE | CVar.CLIENTONLY);

        /// <summary>
        /// Range in pixels for double-clicks
        /// </summary>
        public static readonly CVarDef<int> DoubleClickRange =
            CVarDef.Create("controls.double_click_range", 10, CVar.ARCHIVE | CVar.CLIENTONLY);

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
        /// Force ANGLE to create a context from a D3D11 FL 10_0 device.
        /// </summary>
        public static readonly CVarDef<bool> DisplayAngleForce10_0 =
            CVarDef.Create("display.angle_force_10_0", false, CVar.CLIENTONLY);

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
        /// Enable allowES3OnFL10_0 on ANGLE.
        /// </summary>
        public static readonly CVarDef<bool> DisplayAngleEs3On10_0 =
            CVarDef.Create("display.angle_es3_on_10_0", true, CVar.CLIENTONLY);

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
        /// Diagnostic flag for testing. When using a separate thread for multi-window blitting,
        /// should the worker be unblocked before the SwapBuffers(). Setting to true may improve
        /// performance but may cause crashes or rendering errors.
        /// </summary>
        public static readonly CVarDef<bool> DisplayThreadUnlockBeforeSwap =
            CVarDef.Create("display.thread_unlock_before_swap", false, CVar.CLIENTONLY);

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

        public static readonly CVarDef<string> DisplayWindowingApi =
            CVarDef.Create("display.windowing_api", "glfw", CVar.CLIENTONLY);

        /// <summary>
        /// If true and on Windows 11 Build 22000,
        /// specify <c>DWMWA_USE_IMMERSIVE_DARK_MODE</c> to have dark mode window titles if the system is set to dark mode.
        /// </summary>
        public static readonly CVarDef<bool> DisplayWin11ImmersiveDarkMode =
            CVarDef.Create("display.win11_immersive_dark_mode", true, CVar.CLIENTONLY);

        /// <summary>
        /// If true, run the windowing system in another thread from the game thread.
        /// </summary>
        public static readonly CVarDef<bool> DisplayThreadWindowApi =
            CVarDef.Create("display.thread_window_api", false, CVar.CLIENTONLY);

        /*
         * AUDIO
         */

        /// <summary>
        /// Default limit for concurrently playing an audio file.
        /// </summary>
        public static readonly CVarDef<int> AudioDefaultConcurrent =
            CVarDef.Create("audio.default_concurrent", 16, CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<int> AudioAttenuation =
            CVarDef.Create("audio.attenuation", (int) Attenuation.LinearDistanceClamped, CVar.REPLICATED | CVar.ARCHIVE);

        /// <summary>
        /// Audio device to try to output audio to by default.
        /// </summary>
        public static readonly CVarDef<string> AudioDevice =
            CVarDef.Create("audio.device", string.Empty, CVar.CLIENTONLY);

        /// <summary>
        /// Master volume for audio output.
        /// </summary>
        public static readonly CVarDef<float> AudioMasterVolume =
            CVarDef.Create("audio.mastervolume", 0.50f, CVar.ARCHIVE | CVar.CLIENTONLY);

        /// <summary>
        /// Maximum raycast distance for audio occlusion.
        /// </summary>
        public static readonly CVarDef<float> AudioRaycastLength =
            CVarDef.Create("audio.raycast_length", SharedAudioSystem.DefaultSoundRange, CVar.ARCHIVE | CVar.CLIENTONLY);

        /// <summary>
        /// Maximum offset for audio to be played at from its full duration. If it's past this then the audio won't be played.
        /// </summary>
        public static readonly CVarDef<float> AudioEndBuffer =
            CVarDef.Create("audio.end_buffer", 0.01f, CVar.REPLICATED);

        /// <summary>
        /// Tickrate for audio calculations.
        /// OpenAL recommends 30TPS. This is to avoid running raycasts every frame especially for high-refresh rate monitors.
        /// </summary>
        public static readonly CVarDef<int> AudioTickRate =
            CVarDef.Create("audio.tick_rate", 30, CVar.CLIENTONLY);

        public static readonly CVarDef<float> AudioZOffset =
            CVarDef.Create("audio.z_offset", -5f, CVar.REPLICATED);

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

        /// <summary>
        /// The target minimum ticks per second on the server.
        /// This is used for substepping and will help with clipping/physics issues and such.
        /// Ideally 50-60 is the minimum.
        /// </summary>
        public static readonly CVarDef<int> TargetMinimumTickrate =
            CVarDef.Create("physics.target_minimum_tickrate", 60, CVar.ARCHIVE | CVar.REPLICATED | CVar.SERVER);

        // Grid fixtures
        /// <summary>
        /// I'ma be real with you: the only reason this exists is to get tests working.
        /// </summary>
        public static readonly CVarDef<bool> GenerateGridFixtures =
            CVarDef.Create("physics.grid_fixtures", true, CVar.REPLICATED);

        /// <summary>
        /// Can grids split if not connected by cardinals
        /// </summary>
        public static readonly CVarDef<bool> GridSplitting =
            CVarDef.Create("physics.grid_splitting", true, CVar.ARCHIVE);

        /// <summary>
        /// How much to enlarge grids when determining their fixture bounds.
        /// </summary>
        public static readonly CVarDef<float> GridFixtureEnlargement =
            CVarDef.Create("physics.grid_fixture_enlargement", -PhysicsConstants.PolygonRadius, CVar.ARCHIVE | CVar.REPLICATED);

        // - Sleep
        public static readonly CVarDef<float> AngularSleepTolerance =
            CVarDef.Create("physics.angsleeptol", 0.3f / 180.0f * MathF.PI);

        public static readonly CVarDef<float> LinearSleepTolerance =
            CVarDef.Create("physics.linsleeptol", 0.01f);

        public static readonly CVarDef<bool> SleepAllowed =
            CVarDef.Create("physics.sleepallowed", true);

        // Box2D default is 0.5f
        public static readonly CVarDef<float> TimeToSleep =
            CVarDef.Create("physics.timetosleep", 0.2f);

        // - Solver

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
        /// Default is 400 m/s in-line with Box2c. Box2d used 120m/s.
        /// </remarks>
        public static readonly CVarDef<float> MaxLinVelocity =
            CVarDef.Create("physics.maxlinvelocity", 400f, CVar.SERVER | CVar.REPLICATED);

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
         * User interface
         */

        /// <summary>
        /// Change the UITheme
        /// </summary>
        public static readonly CVarDef<string> InterfaceTheme =
            CVarDef.Create("interface.theme", "", CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        /// Should UI have audio at all.
        /// </summary>
        public static readonly CVarDef<bool> InterfaceAudio =
            CVarDef.Create("interface.audio", true, CVar.REPLICATED);

        /// <summary>
        ///Minimum resolution to start clamping autoscale to 1
        /// </summary>
        public static readonly CVarDef<int> ResAutoScaleUpperX =
            CVarDef.Create("interface.resolutionAutoScaleUpperCutoffX",1080 , CVar.CLIENTONLY);

        /// <summary>
        ///Minimum resolution to start clamping autoscale to 1
        /// </summary>
        public static readonly CVarDef<int> ResAutoScaleUpperY =
            CVarDef.Create("interface.resolutionAutoScaleUpperCutoffY",720 , CVar.CLIENTONLY);

        /// <summary>
        ///Maximum resolution to start clamping autos scale to autoscale minimum
        /// </summary>
        public static readonly CVarDef<int> ResAutoScaleLowX =
            CVarDef.Create("interface.resolutionAutoScaleLowerCutoffX",520 , CVar.CLIENTONLY);

        /// <summary>
        ///Maximum resolution to start clamping autos scale to autoscale minimum
        /// </summary>
        public static readonly CVarDef<int> ResAutoScaleLowY =
            CVarDef.Create("interface.resolutionAutoScaleLowerCutoffY",520 , CVar.CLIENTONLY);

        /// <summary>
        /// The minimum ui scale value that autoscale will scale to
        /// </summary>
        public static readonly CVarDef<float> ResAutoScaleMin =
            CVarDef.Create("interface.resolutionAutoScaleMinimum",0.5f , CVar.CLIENTONLY);

        /// <summary>
        ///Enable the UI autoscale system on this control, this will scale down the UI for lower resolutions
        /// </summary>
        public static readonly CVarDef<bool> ResAutoScaleEnabled =
            CVarDef.Create("interface.resolutionAutoScaleEnabled",true , CVar.CLIENTONLY | CVar.ARCHIVE);



        /*
         * DISCORD
         */

        /// <summary>
        /// Enable Discord rich presence integration.
        /// </summary>
        public static readonly CVarDef<bool> DiscordEnabled =
            CVarDef.Create("discord.enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

        public static readonly CVarDef<string> DiscordRichPresenceMainIconId =
            CVarDef.Create("discord.rich_main_icon_id", "devstation", CVar.SERVER | CVar.REPLICATED);

        public static readonly CVarDef<string> DiscordRichPresenceSecondIconId =
            CVarDef.Create("discord.rich_second_icon_id", "logo", CVar.SERVER | CVar.REPLICATED);

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

        /// <summary>
        /// Upper limit on the size of the RSI atlas texture. A lower limit might waste less vram, but start to defeat
        /// the purpose of using an atlas if it gets too small.
        /// </summary>
        public static readonly CVarDef<int> ResRSIAtlasSize =
            CVarDef.Create("res.rsi_atlas_size", 12288, CVar.CLIENTONLY);

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

        /// <summary>
        /// Whether to watch prototype files for prototype reload on the client. Only applies to development builds.
        /// </summary>
        /// <remarks>
        /// The client sends a reload signal to the server on refocus, if you're wondering why this is client-only.
        /// </remarks>
        public static readonly CVarDef<bool> ResPrototypeReloadWatch =
            CVarDef.Create("res.prototype_reload_watch", true, CVar.CLIENTONLY);

        /// <summary>
        /// If true, do a warning check at startup for probably-erroneous file extensions like <c>.yaml</c> in resources.
        /// </summary>
        /// <remarks>
        /// This check is always skipped on <c>FULL_RELEASE</c>.
        /// </remarks>
        public static readonly CVarDef<bool> ResCheckBadFileExtensions =
            CVarDef.Create("res.check_bad_file_extensions", true);

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
            CVarDef.Create("midi.volume", 0.50f, CVar.CLIENTONLY | CVar.ARCHIVE);

        /// <summary>
        /// Controls amount of CPU cores and (by extension) polyphony for Fluidsynth.
        /// </summary>
        /// <remarks>
        /// You probably don't want to set this to be multithreaded, the way Fluidsynth's multithreading works is
        /// probably worse-than-nothing for Robust's usage.
        /// </remarks>
        public static readonly CVarDef<int> MidiParallelism =
            CVarDef.Create("midi.parallelism", 1, CVar.CLIENTONLY | CVar.ARCHIVE);

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
        /// Comma-separated list of tags to advertise via the status server (and therefore, to the hub).
        /// </summary>
        public static readonly CVarDef<string> HubTags =
            CVarDef.Create("hub.tags", "", CVar.ARCHIVE | CVar.SERVERONLY);

        /// <summary>
        /// Comma-separated list of URLs of hub servers to advertise to.
        /// </summary>
        public static readonly CVarDef<string> HubUrls =
            CVarDef.Create("hub.hub_urls", "https://hub.spacestation14.com/", CVar.SERVERONLY);

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

        /*
         * ACZ
         */

        /// <summary>
        /// Whether to use stream compression instead of per-file compression when transmitting ACZ data.
        /// Enabling stream compression significantly reduces bandwidth usage of downloads,
        /// but increases server and launcher CPU load. It also makes final files stored on the client compressed less.
        /// </summary>
        public static readonly CVarDef<bool> AczStreamCompress =
            CVarDef.Create("acz.stream_compress", false, CVar.SERVERONLY);

        /// <summary>
        /// ZSTD Compression level to use when doing ACZ stream compressed.
        /// </summary>
        public static readonly CVarDef<int> AczStreamCompressLevel =
            CVarDef.Create("acz.stream_compress_level", 3, CVar.SERVERONLY);

        /// <summary>
        /// Whether to do compression on individual files for ACZ downloads.
        /// Automatically forced off if stream compression is enabled.
        /// </summary>
        public static readonly CVarDef<bool> AczBlobCompress =
            CVarDef.Create("acz.blob_compress", true, CVar.SERVERONLY);

        /// <summary>
        /// ZSTD Compression level to use for individual file compression.
        /// </summary>
        public static readonly CVarDef<int> AczBlobCompressLevel =
            CVarDef.Create("acz.blob_compress_level", 14, CVar.SERVERONLY);

        // Could consider using a ratio for this?
        /// <summary>
        /// Amount of bytes that need to be saved by compression for the compression to be "worth it".
        /// </summary>
        public static readonly CVarDef<int> AczBlobCompressSaveThreshold =
            CVarDef.Create("acz.blob_compress_save_threshold", 14, CVar.SERVERONLY);

        /// <summary>
        /// Whether to ZSTD compress the ACZ manifest.
        /// If this is enabled (the default) then non-compressed manifest requests will be decompressed live.
        /// </summary>
        public static readonly CVarDef<bool> AczManifestCompress =
            CVarDef.Create("acz.manifest_compress", true, CVar.SERVERONLY);

        /// <summary>
        /// Compression level for ACZ manifest compression.
        /// </summary>
        public static readonly CVarDef<int> AczManifestCompressLevel =
            CVarDef.Create("acz.manifest_compress_level", 14, CVar.SERVERONLY);

        /*
         * CON
         */

        /// <summary>
        /// Add artificial delay (in seconds) to console completion fetching, even for local commands.
        /// </summary>
        /// <remarks>
        /// Intended for debugging the console completion system.
        /// </remarks>
        public static readonly CVarDef<float> ConCompletionDelay =
            CVarDef.Create("con.completion_delay", 0f, CVar.CLIENTONLY);

        /// <summary>
        /// The amount of completions to show in console completion drop downs.
        /// </summary>
        public static readonly CVarDef<int> ConCompletionCount =
            CVarDef.Create("con.completion_count", 15, CVar.CLIENTONLY);

        /// <summary>
        /// The minimum margin of options to keep on either side of the completion cursor, when scrolling through.
        /// </summary>
        public static readonly CVarDef<int> ConCompletionMargin =
            CVarDef.Create("con.completion_margin", 3, CVar.CLIENTONLY);

        /// <summary>
        /// Maximum amount of entries stored by the debug console.
        /// </summary>
        public static readonly CVarDef<int> ConMaxEntries =
            CVarDef.Create("con.max_entries", 3_000, CVar.CLIENTONLY);

        /*
         * THREAD
         */

        /// <summary>
        /// The nominal parallel processing count to use for parallelized operations.
        /// The default of 0 automatically selects the system's processor count.
        /// </summary>
        public static readonly CVarDef<int> ThreadParallelCount =
            CVarDef.Create("thread.parallel_count", 0);

        /*
         * PROF
         */

        /// <summary>
        /// Enabled the profiling system.
        /// </summary>
        public static readonly CVarDef<bool> ProfEnabled = CVarDef.Create("prof.enabled", false);

        /// <summary>
        /// Event log buffer size for the profiling system.
        /// </summary>
        public static readonly CVarDef<int> ProfBufferSize = CVarDef.Create("prof.buffer_size", 8192);

        /// <summary>
        /// Index log buffer size for the profiling system.
        /// </summary>
        public static readonly CVarDef<int> ProfIndexSize = CVarDef.Create("prof.index_size", 128);

        /*
         * Replays
         */

        /// <summary>
        /// A relative path pointing to a folder within the server data directory where all replays will be stored.
        /// </summary>
        public static readonly CVarDef<string> ReplayDirectory = CVarDef.Create("replay.directory", "replays",
            CVar.ARCHIVE);

        /// <summary>
        /// Maximum compressed size of a replay recording (in kilobytes) before recording automatically stops.
        /// </summary>
        public static readonly CVarDef<long> ReplayMaxCompressedSize = CVarDef.Create("replay.max_compressed_size",
            1024L * 512, CVar.ARCHIVE);

        /// <summary>
        /// Maximum uncompressed size of a replay recording (in kilobytes) before recording automatically stops.
        /// </summary>
        public static readonly CVarDef<long> ReplayMaxUncompressedSize = CVarDef.Create("replay.max_uncompressed_size",
            1024L * 1024, CVar.ARCHIVE);

        /// <summary>
        /// Size of the replay (in kilobytes) at which point the replay is considered "large",
        /// and replay clients should enable server GC (if possible) to improve performance.
        /// </summary>
        /// <remarks>
        /// Set to -1 to never make replays use server GC.
        /// </remarks>
        public static readonly CVarDef<long> ReplayServerGCSizeThreshold =
            CVarDef.Create("replay.server_gc_size_threshold", 50L * 1024);

        /// <summary>
        /// Uncompressed size of individual files created by the replay (in kilobytes), where each file contains data
        /// for one or more ticks. Actual files may be slightly larger, this is just a threshold for the file to get
        /// written. After compressing, the files are generally ~30% of their uncompressed size.
        /// </summary>
        public static readonly CVarDef<int> ReplayTickBatchSize = CVarDef.Create("replay.replay_tick_batchSize",
            1024, CVar.ARCHIVE);

        /// <summary>
        /// The max amount of pending write commands while recording replays.
        /// </summary>
        public static readonly CVarDef<int> ReplayWriteChannelSize = CVarDef.Create("replay.write_channel_size", 5);

        /// <summary>
        /// Whether or not server-side replay recording is enabled.
        /// </summary>
        public static readonly CVarDef<bool> ReplayServerRecordingEnabled = CVarDef.Create(
            "replay.server_recording_enabled",
            true,
            CVar.SERVERONLY | CVar.ARCHIVE);

        /// <summary>
        /// Whether or not client-side replay recording is enabled.
        /// </summary>
        public static readonly CVarDef<bool> ReplayClientRecordingEnabled = CVarDef.Create(
            "replay.client_recording_enabled",
            true,
            CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

        /// <summary>
        /// How many milliseconds we will spend moving forward from the nearest checkpoint or current position.
        /// We will spend this time when scrubbing the timeline per game tick. This limits CPU usage / locking up and
        /// improves responsiveness
        /// </summary>
        public static readonly CVarDef<int> ReplayMaxScrubTime = CVarDef.Create("replay.max_scrub_time", 10);

        /// <summary>
        /// Determines the threshold before visual events (muzzle flashes, chat pop-ups, etc) are suppressed when
        /// jumping forward in time. Jumps larger than this will simply skip directly to the target tick.
        /// </summary>
        public static readonly CVarDef<int> ReplaySkipThreshold = CVarDef.Create("replay.skip_threshold", 30);

        /// <summary>
        /// Minimum number of ticks before a new checkpoint tick is generated (overrides SpawnThreshold and StateThreshold)
        /// </summary>
        public static readonly CVarDef<int> CheckpointMinInterval = CVarDef.Create("replay.checkpoint_min_interval", 60);

        /// <summary>
        /// Maximum number of ticks before a new checkpoint tick is generated.
        /// </summary>
        public static readonly CVarDef<int> CheckpointInterval = CVarDef.Create("replay.checkpoint_interval", 500);

        /// <summary>
        /// Maximum number of entities that can be spawned before a new checkpoint tick is generated.
        /// </summary>
        public static readonly CVarDef<int> CheckpointEntitySpawnThreshold = CVarDef.Create("replay.checkpoint_entity_spawn_threshold", 1000);

        /// <summary>
        /// Maximum number of entity states that can be applied before a new checkpoint tick is generated.
        /// </summary>
        public static readonly CVarDef<int> CheckpointEntityStateThreshold = CVarDef.Create("replay.checkpoint_entity_state_threshold", 50 * 600);

        /// <summary>
        /// Whether or not to constantly apply game states while using something like a slider to scrub through replays.
        /// If false, this will only jump to a point in time when the scrubbing ends.
        /// </summary>
        public static readonly CVarDef<bool> ReplayDynamicalScrubbing = CVarDef.Create("replay.dynamical_scrubbing", true);

        /// <summary>
        /// When recording replays, should we attempt to make a valid content bundle that can be directly executed by
        /// the launcher?
        /// </summary>
        /// <remarks>
        /// This requires the server's build information to be sufficiently filled out.
        /// </remarks>
        public static readonly CVarDef<bool> ReplayMakeContentBundle =
            CVarDef.Create("replay.make_content_bundle", true);

        /// <summary>
        /// If true, this will cause the replay client to ignore some errors while loading a replay file.
        /// </summary>
        /// <remarks>
        /// This might make otherwise broken replays playable, but ignoring these errors is also very likely to
        /// cause unexpected and confusing errors elsewhere. By default this is disabled so that users report the
        /// original exception rather than sending people on a wild-goose chase to find a non-existent bug.
        /// </remarks>
        public static readonly CVarDef<bool> ReplayIgnoreErrors =
            CVarDef.Create("replay.ignore_errors", false, CVar.CLIENTONLY);

        /*
         * CFG
         */

        /// <summary>
        /// If set, check for any unknown CVars once the game is initialized to try the spot any unknown ones.
        /// </summary>
        /// <remarks>
        /// CVars can be dynamically registered instead of just being statically known ahead of time,
        /// so the engine is not capable of immediately telling if a CVar is a typo or such.
        /// This check after game initialization assumes all CVars have been registered,
        /// and will complain if anything unknown is found (probably indicating a typo of some kind).
        /// </remarks>
        public static readonly CVarDef<bool> CfgCheckUnused = CVarDef.Create("cfg.check_unused", true);

        /*
        * Network Resource Manager
        */

        /// <summary>
        /// Controls whether new resources can be uploaded by admins.
        /// Does not prevent already uploaded resources from being sent.
        /// </summary>
        public static readonly CVarDef<bool> ResourceUploadingEnabled =
            CVarDef.Create("netres.enabled", true, CVar.REPLICATED | CVar.SERVER);

        /// <summary>
        /// Controls the data size limit in megabytes for uploaded resources. If they're too big, they will be dropped.
        /// Set to zero or a negative value to disable limit.
        /// </summary>
        public static readonly CVarDef<float> ResourceUploadingLimitMb =
            CVarDef.Create("netres.limit", 3f, CVar.REPLICATED | CVar.SERVER);

        /*
         * LAUNCH
         * CVars relating to how the client is launched. Primarily set from the launcher.
         */

        /// <summary>
        /// Game was launched from the launcher.
        /// </summary>
        /// <remarks>
        /// The game should not try to automatically connect to a server, there's other variables for that.
        /// </remarks>
        public static readonly CVarDef<bool> LaunchLauncher =
            CVarDef.Create("launch.launcher", false, CVar.CLIENTONLY);

        /// <summary>
        /// Game was launched from a content bundle.
        /// </summary>
        public static readonly CVarDef<bool> LaunchContentBundle =
            CVarDef.Create("launch.content_bundle", false, CVar.CLIENTONLY);

        /*
         * TOOLSHED
         */

        /// <summary>
        ///     The max range that can be passed to the nearby toolshed command.
        ///     Any higher value will cause an exception.
        /// </summary>
        public static readonly CVarDef<int> ToolshedNearbyLimit =
            CVarDef.Create("toolshed.nearby_limit", 200, CVar.SERVER | CVar.REPLICATED);

        /// <summary>
        ///     The max amount of entities that can be passed to the nearby toolshed command.
        ///     Any higher value will cause an exception.
        /// </summary>
        public static readonly CVarDef<int> ToolshedNearbyEntitiesLimit =
            CVarDef.Create("toolshed.nearby_entities_limit", 5, CVar.SERVER | CVar.REPLICATED);

        /*
         * Localization
         */

        public static readonly CVarDef<string> LocCultureName =
            CVarDef.Create("loc.culture_name", "en-US", CVar.ARCHIVE);
    }
}
