using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using RLogLevel = Robust.Shared.Log.LogLevel;

#pragma warning disable CS8500

namespace Robust.Client.Graphics.Rhi.WebGpu;

internal sealed unsafe partial class RhiWebGpu : RhiBase
{
    private readonly ISawmill _sawmill;
    private readonly ISawmill _apiLogSawmill;

    private WGPUInstance _wgpuInstance;
    private WGPUAdapter _wgpuAdapter;
    private WGPUDevice _wgpuDevice;
    private WGPUQueue _wgpuQueue;

    private RhiLimits? _deviceLimits;
    private RhiAdapterInfo? _adapterProperties;
    private string _description = "not initialized";

    public override RhiLimits DeviceLimits =>
        _deviceLimits ?? throw new InvalidOperationException("Not initialized yet");

    public override RhiAdapterInfo AdapterInfo =>
        _adapterProperties ?? throw new InvalidOperationException("Not initialized yet");

    public override string Description => _description;

    public RhiWebGpu(IDependencyCollection dependencies)
    {
        var logMgr = dependencies.Resolve<ILogManager>();

        _sawmill = logMgr.GetSawmill("clyde.rhi.webGpu");
        _apiLogSawmill = logMgr.GetSawmill("clyde.rhi.webGpu.apiLog");

        Queue = new RhiQueue(this, default);
    }

    internal override void Init(in RhiInitParams initParams, out WindowData windowData)
    {
        _sawmill.Info("Initializing WebGPU RHI!");

        InitInstance(in initParams);

        windowData = CreateSurfaceForWindow(in initParams.MainWindowSurfaceParams);

        _sawmill.Debug("WebGPU main surface created!");

        InitAdapterAndDevice(in initParams, windowData.Surface);

        ConfigureSurface(windowData, initParams.MainWindowSize);
    }

    private void InitInstance(in RhiInitParams initParams)
    {
        var wgpuVersion = WgpuVersionToString(wgpuGetVersion());
        _sawmill.Debug($"wgpu-native loaded, version: {wgpuVersion}");

        _description = $"WebGPU (wgpu-native {wgpuVersion})";

        InitLogging();

        Span<byte> buffer = stackalloc byte[128];
        var pInstanceDescriptor = BumpAllocate<WGPUInstanceDescriptor>(ref buffer);

        // Specify instance extras for wgpu-native.
        var pInstanceExtras = BumpAllocate<WGPUInstanceExtras>(ref buffer);
        pInstanceDescriptor->nextInChain = (WGPUChainedStruct*)pInstanceExtras;
        pInstanceExtras->chain.sType = (WGPUSType)WGPUNativeSType.WGPUSType_InstanceExtras;
        pInstanceExtras->backends = (uint)GetInstanceBackendCfg(initParams.Backends);

        _wgpuInstance = wgpuCreateInstance(pInstanceDescriptor);

        _sawmill.Debug("WebGPU instance created!");
    }

    private ulong GetInstanceBackendCfg(string backendCvar)
    {
        if (backendCvar == "all")
            return WGPUInstanceBackend_Primary | WGPUInstanceBackend_Secondary;

        var backends = 0ul;
        foreach (var opt in backendCvar.Split(","))
        {
            backends |= opt switch
            {
                "vulkan" => WGPUInstanceBackend_Vulkan,
                "gl" => WGPUInstanceBackend_GL,
                "metal" => WGPUInstanceBackend_Metal,
                "dx12" => WGPUInstanceBackend_DX12,
                "dx11" => WGPUInstanceBackend_DX11,
                "browser" => WGPUInstanceBackend_BrowserWebGPU,
                _ => throw new ArgumentException($"Unknown wgpu backend: '{opt}'")
            };
        }

        return backends;
    }

    private void InitAdapterAndDevice(in RhiInitParams initParams, WGPUSurface forSurface)
    {
        var powerPreference = ValidatePowerPreference(initParams.PowerPreference);

        var requestAdapterOptions = new WGPURequestAdapterOptions
        {
            compatibleSurface = forSurface,
            powerPreference = powerPreference
        };

        WgpuRequestAdapterResult result;
        wgpuInstanceRequestAdapter(
            _wgpuInstance,
            &requestAdapterOptions,
            new WGPURequestAdapterCallbackInfo
            {
                callback = &WgpuRequestAdapterCallback,
                userdata1 = &result,
            });

        if (result.Status != WGPURequestAdapterStatus.WGPURequestAdapterStatus_Success)
            throw new RhiException($"Adapter request failed: {result.Message}");

        _sawmill.Debug("WebGPU adapter created!");

        _wgpuAdapter = result.Adapter.P;

        WGPUAdapterInfo adapterProps = default;
        wgpuAdapterGetInfo(_wgpuAdapter, &adapterProps);

        WGPULimits adapterLimits = default;
        wgpuAdapterGetLimits(_wgpuAdapter, &adapterLimits);

        _sawmill.Debug($"adapter device: {GetString(adapterProps.device)}");
        _sawmill.Debug($"adapter vendor: {GetString(adapterProps.vendor)} ({adapterProps.vendorID})");
        _sawmill.Debug($"adapter description: {GetString(adapterProps.description)}");
        _sawmill.Debug($"adapter architecture: {GetString(adapterProps.architecture)}");
        _sawmill.Debug($"adapter backend: {adapterProps.backendType}");
        _sawmill.Debug($"adapter type: {adapterProps.adapterType}");
        _sawmill.Debug($"adapter UBO alignment: {adapterLimits.minUniformBufferOffsetAlignment}");

        _adapterProperties = new RhiAdapterInfo(
            adapterProps.vendorID,
            adapterProps.deviceID,
            GetString(adapterProps.vendor) ?? "",
            GetString(adapterProps.architecture) ?? "",
            GetString(adapterProps.device) ?? "",
            GetString(adapterProps.description) ?? "",
            (RhiAdapterType) adapterProps.adapterType,
            (RhiBackendType) adapterProps.backendType
        );

        // Default limits, from WebGPU spec.
        var requiredLimits = new WGPULimits();
        if (false)
        {
            // GLES3.0
            requiredLimits.maxComputeWorkgroupStorageSize = 16384;
            requiredLimits.maxComputeInvocationsPerWorkgroup = 256;
            requiredLimits.maxComputeWorkgroupSizeX = 256;
            requiredLimits.maxComputeWorkgroupSizeY = 256;
            requiredLimits.maxComputeWorkgroupSizeZ = 256;
            requiredLimits.maxComputeWorkgroupsPerDimension = 65536;
            requiredLimits.maxDynamicStorageBuffersPerPipelineLayout = 0;
            requiredLimits.maxStorageBuffersPerShaderStage = 4;
            requiredLimits.maxStorageBufferBindingSize = 134217728;
        }

        // Required minimums
        requiredLimits.minStorageBufferOffsetAlignment = 256;
        requiredLimits.minUniformBufferOffsetAlignment = 256;

        requiredLimits.maxTextureDimension1D = 8192;
        requiredLimits.maxTextureDimension2D = 8192;
        requiredLimits.maxTextureDimension3D = 2048;
        requiredLimits.maxTextureArrayLayers = 256;
        requiredLimits.maxBindGroups = 4;
        requiredLimits.maxBindingsPerBindGroup = 1000;
        requiredLimits.maxDynamicUniformBuffersPerPipelineLayout = 8;
        requiredLimits.maxSampledTexturesPerShaderStage = 16;
        requiredLimits.maxSamplersPerShaderStage = 16;
        requiredLimits.maxUniformBuffersPerShaderStage = 12;
        requiredLimits.maxUniformBufferBindingSize = 65536;
        requiredLimits.maxVertexBuffers = 8;
        requiredLimits.maxVertexAttributes = 16;
        requiredLimits.maxVertexBufferArrayStride = 2048;
        requiredLimits.maxInterStageShaderVariables = 16;
        requiredLimits.maxColorAttachments = 8;
        requiredLimits.maxColorAttachmentBytesPerSample = 32;
        requiredLimits.maxBufferSize = 268435456;

        // Custom limits
        // Take as low UBO alignment as we can get.
        requiredLimits.minUniformBufferOffsetAlignment = adapterLimits.minUniformBufferOffsetAlignment;

        // TODO: clear this.
        var errorGCHandle = GCHandle.Alloc(this);

        var deviceDesc = new WGPUDeviceDescriptor();
        deviceDesc.requiredLimits = &requiredLimits;
        deviceDesc.uncapturedErrorCallbackInfo = new WGPUUncapturedErrorCallbackInfo
        {
            callback = &UncapturedErrorCallback,
            userdata1 = (void*)GCHandle.ToIntPtr(errorGCHandle),
        };
        WgpuRequestDeviceResult deviceResult;
        wgpuAdapterRequestDevice(
            _wgpuAdapter,
            &deviceDesc,
            new WGPURequestDeviceCallbackInfo
            {
                callback = &WgpuRequestDeviceCallback,
                userdata1 = &deviceResult
            });

        if (deviceResult.Status != WGPURequestDeviceStatus.WGPURequestDeviceStatus_Success)
            throw new Exception($"Device request failed: {deviceResult.Message}");

        _sawmill.Debug("WebGPU device created!");

        _wgpuDevice = deviceResult.Device;
        _wgpuQueue = wgpuDeviceGetQueue(_wgpuDevice);

        _deviceLimits = new RhiLimits(
            requiredLimits.maxTextureDimension1D,
            requiredLimits.maxTextureDimension2D,
            requiredLimits.maxTextureDimension3D,
            requiredLimits.maxTextureArrayLayers,
            requiredLimits.maxBindGroups,
            requiredLimits.maxBindingsPerBindGroup,
            requiredLimits.maxDynamicUniformBuffersPerPipelineLayout,
            requiredLimits.maxDynamicStorageBuffersPerPipelineLayout,
            requiredLimits.maxSampledTexturesPerShaderStage,
            requiredLimits.maxSamplersPerShaderStage,
            requiredLimits.maxStorageBuffersPerShaderStage,
            requiredLimits.maxStorageTexturesPerShaderStage,
            requiredLimits.maxUniformBuffersPerShaderStage,
            requiredLimits.maxUniformBufferBindingSize,
            requiredLimits.maxStorageBufferBindingSize,
            requiredLimits.minUniformBufferOffsetAlignment,
            requiredLimits.minStorageBufferOffsetAlignment,
            requiredLimits.maxVertexBuffers,
            requiredLimits.maxBufferSize,
            requiredLimits.maxVertexAttributes,
            requiredLimits.maxVertexBufferArrayStride,
            requiredLimits.maxInterStageShaderVariables,
            requiredLimits.maxColorAttachments,
            requiredLimits.maxColorAttachmentBytesPerSample,
            requiredLimits.maxComputeWorkgroupStorageSize,
            requiredLimits.maxComputeInvocationsPerWorkgroup,
            requiredLimits.maxComputeWorkgroupSizeX,
            requiredLimits.maxComputeWorkgroupSizeY,
            requiredLimits.maxComputeWorkgroupSizeZ,
            requiredLimits.maxComputeWorkgroupsPerDimension
        );
    }

    private void InitLogging()
    {
        // TODO: clear this.
        var gcHandle = GCHandle.Alloc(this);

        wgpuSetLogCallback(&LogCallback, (void*)GCHandle.ToIntPtr(gcHandle));
        wgpuSetLogLevel(WGPULogLevel.WGPULogLevel_Warn);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void LogCallback(WGPULogLevel level, WGPUStringView message, void* userdata)
    {
        var self = (RhiWebGpu)GCHandle.FromIntPtr((nint)userdata).Target!;
        var messageString = GetString(message)!;

        var robustLevel = level switch
        {
            WGPULogLevel.WGPULogLevel_Error => RLogLevel.Error,
            WGPULogLevel.WGPULogLevel_Warn => RLogLevel.Warning,
            WGPULogLevel.WGPULogLevel_Info => RLogLevel.Info,
            WGPULogLevel.WGPULogLevel_Debug => RLogLevel.Debug,
            WGPULogLevel.WGPULogLevel_Trace or _ => RLogLevel.Verbose,
        };

        self._apiLogSawmill.Log(robustLevel, messageString);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void UncapturedErrorCallback(WGPUDevice* device, WGPUErrorType level, WGPUStringView message, void* userdata1, void* userdata2)
    {
        var self = (RhiWebGpu)GCHandle.FromIntPtr((nint)userdata1).Target!;
        var messageString = GetString(message);

        self._apiLogSawmill.Error(messageString ?? "Unknown error");
    }

    internal override void Shutdown()
    {
    }

    private record struct WgpuRequestAdapterResult(WGPURequestAdapterStatus Status, Ptr<WGPUAdapterImpl> Adapter, string Message);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void WgpuRequestAdapterCallback(
        WGPURequestAdapterStatus status,
        WGPUAdapter adapter,
        WGPUStringView message,
        void* userdata1,
        void* userdata2)
    {
        *(WgpuRequestAdapterResult*)userdata1 = new WgpuRequestAdapterResult(
            status,
            adapter,
            GetString(message) ?? "");
    }

    private record struct WgpuRequestDeviceResult(WGPURequestDeviceStatus Status, Ptr<WGPUDeviceImpl> Device, string Message);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void WgpuRequestDeviceCallback(
        WGPURequestDeviceStatus status,
        WGPUDevice device,
        WGPUStringView message,
        void* userdata1,
        void* userdata2)
    {
        *(WgpuRequestDeviceResult*)userdata1 = new WgpuRequestDeviceResult(
            status,
            device,
            GetString(message) ?? "");
    }
}
