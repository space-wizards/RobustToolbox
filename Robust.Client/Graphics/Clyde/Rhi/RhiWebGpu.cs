using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;
using Silk.NET.WebGPU;
using Silk.NET.WebGPU.Extensions.WGPU;
using LogLevel = Silk.NET.WebGPU.Extensions.WGPU.LogLevel;
using RLogLevel = Robust.Shared.Log.LogLevel;
using RColor = Robust.Shared.Maths.Color;

#pragma warning disable CS8500

namespace Robust.Client.Graphics.Clyde.Rhi;

internal sealed unsafe partial class RhiWebGpu : RhiBase
{
    private readonly Clyde _clyde;

    private readonly ISawmill _sawmill;
    private readonly ISawmill _apiLogSawmill;

    private WebGPU _webGpu = default!;
    private Wgpu _wgpu = default!;
    private Instance* _wgpuInstance;
    private Adapter* _wgpuAdapter;
    private Device* _wgpuDevice;
    private Queue* _wgpuQueue;

    public RhiWebGpu(Clyde clyde, IDependencyCollection dependencies)
    {
        var logMgr = dependencies.Resolve<ILogManager>();

        _clyde = clyde;
        _sawmill = logMgr.GetSawmill("clyde.rhi.webGpu");
        _apiLogSawmill = logMgr.GetSawmill("clyde.rhi.webGpu.apiLog");

        Queue = new RhiQueue(this, default);
    }

    internal override void Init()
    {
        _sawmill.Info("Initializing WebGPU RHI!");

        InitInstance();

        CreateSurfaceForWindow(_clyde._mainWindow!);

        _sawmill.Debug("WebGPU main surface created!");

        InitAdapterAndDevice(_clyde._mainWindow!.RhiWebGpuData!.Surface);

        CreateSwapChainForWindow(_clyde._mainWindow!);
    }

    private void InitInstance()
    {
        var context = WebGPU.CreateDefaultContext(new[] { "wgpu_native.dll" });
        _webGpu = new WebGPU(context);
        _wgpu = new Wgpu(context);

        _sawmill.Debug($"wgpu-native loaded, version: {WgpuVersionToString(_wgpu.GetVersion())}");

        // InitLogging();

        var instanceDescriptor = new InstanceDescriptor();
        _wgpuInstance = _webGpu.CreateInstance(&instanceDescriptor);

        _sawmill.Debug("WebGPU instance created!");
    }

    private void InitAdapterAndDevice(Surface* forSurface)
    {
        var requestAdapterOptions = new RequestAdapterOptions
        {
            CompatibleSurface = forSurface,
            PowerPreference = PowerPreference.HighPerformance
        };

        WgpuRequestAdapterResult result;
        _webGpu.InstanceRequestAdapter(
            _wgpuInstance,
            &requestAdapterOptions,
            new PfnRequestAdapterCallback(&WgpuRequestAdapterCallback),
            &result);

        if (result.Status != RequestAdapterStatus.Success)
            throw new Exception($"Adapter request failed: {result.Message}");

        _sawmill.Debug("WebGPU adapter created!");

        _wgpuAdapter = result.Adapter.P;

        AdapterProperties adapterProps = default;
        _webGpu.AdapterGetProperties(_wgpuAdapter, &adapterProps);

        _sawmill.Debug($"adapter name: {MarshalFromString(adapterProps.Name)}");
        _sawmill.Debug($"adapter vendor: {MarshalFromString(adapterProps.VendorName)} ({adapterProps.VendorID})");
        _sawmill.Debug($"adapter driver: {MarshalFromString(adapterProps.DriverDescription)}");
        _sawmill.Debug($"adapter architecture: {MarshalFromString(adapterProps.Architecture)}");
        _sawmill.Debug($"adapter backend: {adapterProps.BackendType}");
        _sawmill.Debug($"adapter type: {adapterProps.AdapterType}");

        var deviceDesc = new DeviceDescriptor();
        WgpuRequestDeviceResult deviceResult;
        _webGpu.AdapterRequestDevice(
            _wgpuAdapter,
            &deviceDesc,
            new PfnRequestDeviceCallback(&WgpuRequestDeviceCallback),
            &deviceResult);

        if (deviceResult.Status != RequestDeviceStatus.Success)
            throw new Exception($"Device request failed: {deviceResult.Message}");

        _sawmill.Debug("WebGPU device created!");

        _wgpuDevice = deviceResult.Device;
        _wgpuQueue = _webGpu.DeviceGetQueue(_wgpuDevice);

        InitErrorCallback();
    }


    private void InitLogging()
    {
        // TODO: clear this.
        var gcHandle = GCHandle.Alloc(this);

        _wgpu.SetLogCallback(new PfnLogCallback(&LogCallback), (void*)GCHandle.ToIntPtr(gcHandle));
        _wgpu.SetLogLevel(LogLevel.Trace);
    }

    private void InitErrorCallback()
    {
        // TODO: clear this.
        var gcHandle = GCHandle.Alloc(this);

        _webGpu.DeviceSetUncapturedErrorCallback(
            _wgpuDevice,
            new PfnErrorCallback(&UncapturedErrorCallback),
            (void*) GCHandle.ToIntPtr(gcHandle));
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void LogCallback(LogLevel level, byte* message, void* userdata)
    {
        var self = (RhiWebGpu)GCHandle.FromIntPtr((nint)userdata).Target!;
        var messageString = Marshal.PtrToStringUTF8((nint)message)!;

        var robustLevel = level switch
        {
            LogLevel.Error => RLogLevel.Error,
            LogLevel.Warn => RLogLevel.Warning,
            LogLevel.Info => RLogLevel.Info,
            LogLevel.Debug => RLogLevel.Debug,
            LogLevel.Trace or _ => RLogLevel.Verbose,
        };

        self._apiLogSawmill.Log(robustLevel, messageString);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void UncapturedErrorCallback(ErrorType level, byte* message, void* userdata)
    {
        var self = (RhiWebGpu)GCHandle.FromIntPtr((nint)userdata).Target!;
        var messageString = Marshal.PtrToStringUTF8((nint)message)!;

        self._apiLogSawmill.Error(messageString);
    }

    internal override void Shutdown()
    {
    }

    private record struct WgpuRequestAdapterResult(RequestAdapterStatus Status, Ptr<Adapter> Adapter, string Message);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void WgpuRequestAdapterCallback(
        RequestAdapterStatus status,
        Adapter* adapter,
        byte* message,
        void* userdata)
    {
        *(WgpuRequestAdapterResult*)userdata = new WgpuRequestAdapterResult(
            status,
            adapter,
            MarshalFromString(message));
    }

    private record struct WgpuRequestDeviceResult(RequestDeviceStatus Status, Ptr<Device> Device, string Message);

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void WgpuRequestDeviceCallback(
        RequestDeviceStatus status,
        Device* device,
        byte* message,
        void* userdata)
    {
        *(WgpuRequestDeviceResult*)userdata = new WgpuRequestDeviceResult(
            status,
            device,
            MarshalFromString(message));
    }
}
