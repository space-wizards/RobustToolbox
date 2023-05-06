using System;
using System.Collections.Generic;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.DirectX.D3D_FEATURE_LEVEL;
using static TerraFX.Interop.DirectX.D3D_DRIVER_TYPE;
using static TerraFX.Interop.Windows.Windows;
using static TerraFX.Interop.DirectX.DirectX;
using static TerraFX.Interop.DirectX.D3D11;
using static TerraFX.Interop.DirectX.D3D11_CREATE_DEVICE_FLAG;
using static TerraFX.Interop.DirectX.DXGI;
using static TerraFX.Interop.DirectX.DXGI_FORMAT;

namespace Robust.Client.Graphics.Clyde.Rhi;

internal sealed unsafe partial class RhiD3D11 : RhiBase
{
    private readonly Clyde _clyde;
    private readonly IConfigurationManager _cfg;
    private readonly ISawmill _sawmill;

    private IDXGIFactory2* _dxgiFactory;
    private IDXGIAdapter1* _dxgiAdapter;
    private ID3D11Device* _device;
    private ID3D11DeviceContext* _immediateContext;

    public RhiD3D11(Clyde clyde, IDependencyCollection dependencies)
    {
        var logMgr = dependencies.Resolve<ILogManager>();
        _cfg = dependencies.Resolve<IConfigurationManager>();

        _clyde = clyde;
        _sawmill = logMgr.GetSawmill("clyde.rhi.d3d11");
    }

    public override void Init()
    {
        _sawmill.Info("Initializing D3D11 RHI!");

        CreateDxgiFactory();

        InitAdapterAndDevice();

        DescribeSelectedDevice();

        CreateSwapChainForWindow(_clyde._mainWindow!);

        {
            var swapChain = _clyde._mainWindow!.RhiD3D11Data!.SwapChain;

            ID3D11Texture2D* buffer;
            ThrowIfFailed(
                "GetBuffer",
                swapChain->GetBuffer(0, __uuidof<ID3D11Texture2D>(), (void**) &buffer));

            var rtvDesc = new D3D11_RENDER_TARGET_VIEW_DESC
            {
                Format = DXGI_FORMAT_B8G8R8A8_UNORM_SRGB,
                ViewDimension = D3D11_RTV_DIMENSION.D3D11_RTV_DIMENSION_TEXTURE2D,
                Texture2D = new D3D11_TEX2D_RTV
                {
                    MipSlice = 0
                }
            };

            ID3D11RenderTargetView* rtv;
            var res = _device->CreateRenderTargetView(
                (ID3D11Resource*)buffer,
                &rtvDesc,
                &rtv
            );
            ThrowIfFailed("CreateRenderTargetView", res);

            buffer->Release();

            var clearColor = Color.HotPink;
            _immediateContext->ClearRenderTargetView(rtv, (float*) &clearColor);

            swapChain->Present(1, 0);
            rtv->Release();
        }
    }

    private void CreateDxgiFactory()
    {
        var debugDevice = _cfg.GetCVar(CVars.DisplayD3D11DebugDevice);
        if (debugDevice && OperatingSystem.IsWindowsVersionAtLeast(6, 3))
        {
            // Windows 8.1, use CreateDXGIFactory2 to create a debug device.
            fixed (IDXGIFactory2** pFactory = &_dxgiFactory)
            {
                ThrowIfFailed(
                    nameof(CreateDXGIFactory2),
                    CreateDXGIFactory2(DXGI_CREATE_FACTORY_DEBUG, __uuidof<IDXGIFactory2>(), (void**)pFactory));
            }
        }

        IDXGIFactory1* dxgiFactory;
        ThrowIfFailed(
            nameof(CreateDXGIFactory1),
            CreateDXGIFactory1(__uuidof<IDXGIFactory1>(), (void**)&dxgiFactory));

        fixed (IDXGIFactory2** pFactory = &_dxgiFactory)
        {
            ThrowIfFailed(
                "QueryInterface for IDXGIFactory2",
                dxgiFactory->QueryInterface(__uuidof<IDXGIFactory2>(), (void**) pFactory));
        }

        dxgiFactory->Release();
    }

    private void InitAdapterAndDevice()
    {
        Span<D3D_FEATURE_LEVEL> featureLevels = stackalloc D3D_FEATURE_LEVEL[]
        {
            D3D_FEATURE_LEVEL_11_0,
        };

        var pickedAdapter = PickAdapter();

        var debugDevice = _cfg.GetCVar(CVars.DisplayD3D11DebugDevice);

        fixed (ID3D11Device** device = &_device)
        fixed (ID3D11DeviceContext** immediateContext = &_immediateContext)
        fixed (D3D_FEATURE_LEVEL* fl = &featureLevels[0])
        {
            D3D11_CREATE_DEVICE_FLAG flags = 0;
            if (debugDevice)
                flags |= D3D11_CREATE_DEVICE_DEBUG;

            ThrowIfFailed("D3D11CreateDevice", D3D11CreateDevice(
                (IDXGIAdapter*)pickedAdapter,
                pickedAdapter == null ? D3D_DRIVER_TYPE_HARDWARE : D3D_DRIVER_TYPE_UNKNOWN,
                HMODULE.NULL,
                (uint) flags,
                fl,
                (uint)featureLevels.Length,
                D3D11_SDK_VERSION,
                device,
                null,
                immediateContext
            ));
        }

        if (pickedAdapter != null)
            pickedAdapter->Release();

        IDXGIDevice1* dxgiDevice;
        ThrowIfFailed("QueryInterface", _device->QueryInterface(__uuidof<IDXGIDevice1>(), (void**)&dxgiDevice));

        fixed (IDXGIAdapter1** ptrAdapter = &_dxgiAdapter)
        {
            ThrowIfFailed("GetParent", dxgiDevice->GetParent(__uuidof<IDXGIAdapter1>(), (void**)ptrAdapter));
        }

        dxgiDevice->Release();

        _sawmill.Debug("Created D3D11 device!");
    }

    private IDXGIAdapter1* PickAdapter()
    {
        // Try to find adapter by name if specified in config.
        var adapterName = _cfg.GetCVar(CVars.DisplayAdapter);

        IDXGIAdapter1* adapter = null;
        if (adapterName != "")
        {
            adapter = TryFindAdapterWithName(adapterName);

            if (adapter != null)
            {
                _sawmill.Debug("Found requested adapter with name: {AdapterName}", adapterName);
                return adapter;
            }

            _sawmill.Warning("Unable to find adapter with requested name: {AdapterName}", adapterName);
        }

#pragma warning disable CS0162
        IDXGIFactory6* factory6;
        if (adapter == null && _dxgiFactory->QueryInterface(__uuidof<IDXGIFactory6>(), (void**)&factory6) == 0)
        {
            var gpuPref = (DXGI_GPU_PREFERENCE)_cfg.GetCVar(CVars.DisplayGpuPreference);
            _sawmill.Debug("Picking adapter via GPU preference: {GpuPreference}", gpuPref);

            for (var adapterIndex = 0u;
                 factory6->EnumAdapterByGpuPreference(
                     adapterIndex,
                     gpuPref,
                     __uuidof<IDXGIAdapter1>(),
                     (void**)&adapter) != DXGI_ERROR_NOT_FOUND;
                 adapterIndex++)
            {
                return _dxgiAdapter;
            }

            factory6->Release();
        }
#pragma warning restore CS0162

        return null;
    }

    private IDXGIAdapter1* TryFindAdapterWithName(string name)
    {
        uint idx = 0;

        while (true)
        {
            IDXGIAdapter1* adapter;
            var hr = _dxgiFactory->EnumAdapters1(idx++, &adapter);
            if (hr == DXGI_ERROR_NOT_FOUND)
                break;

            ThrowIfFailed("EnumAdapters1", hr);

            DXGI_ADAPTER_DESC1 desc;
            ThrowIfFailed("GetDesc1", adapter->GetDesc1(&desc));

            var descName = new ReadOnlySpan<char>(desc.Description, 128);

            if (descName.StartsWith(name))
                return adapter;

            adapter->Release();
        }

        return null;
    }

    private void DescribeSelectedDevice()
    {
        DXGI_ADAPTER_DESC1 desc;
        ThrowIfFailed("IDXGIAdapter1::GetDesc1", _dxgiAdapter->GetDesc1(&desc));

        var descNameSpan = new ReadOnlySpan<char>(desc.Description, 128);
        var descName = descNameSpan.TrimEnd('\0').ToString();

        var vendorName = KnownPciDeviceVendors.GetValueOrDefault(desc.VendorId);
        _sawmill.Debug($"Device FL: {_device->GetFeatureLevel()}");
        _sawmill.Debug($"Adapter desc: {descName}");
        _sawmill.Debug($"Adapter vendor: {desc.VendorId:X4} ({vendorName})");
        _sawmill.Debug($"Adapter dedicated video memory: {ByteHelpers.FormatBytes((long)desc.DedicatedVideoMemory)}");
        _sawmill.Debug($"Adapter dedicated system memory: {ByteHelpers.FormatBytes((long)desc.DedicatedSystemMemory)}");
        _sawmill.Debug($"Adapter shared system memory: {ByteHelpers.FormatBytes((long)desc.SharedSystemMemory)}");
    }

    public override void Shutdown()
    {
        _clyde._mainWindow!.RhiD3D11Data!.SwapChain->Release();

        _device->Release();
        _immediateContext->Release();
        _dxgiAdapter->Release();
        _dxgiFactory->Release();
    }
}
