using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Robust.Client.Interop.RobustNative.Wesl.Gen;
using Robust.Shared.Collections;
using Robust.Shared.ContentPack;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Client.Graphics;

internal sealed class ShaderCompiler : IShaderCompiler, IDisposable
{
    private readonly IResourceManager _resourceManager;
    private readonly ISawmill _sawmill;

    private bool _disposed;
    private GCHandle _resolverGCHandle;
    private readonly ReaderWriterLockSlim _rwLock = new();

    private ValueList<PackageDefinition> _packages;

    public ShaderCompiler(IResourceManager resourceManager, ILogManager logManager)
    {
        _resolverGCHandle = GCHandle.Alloc(this);
        _resourceManager = resourceManager;
        _sawmill = logManager.GetSawmill("shader");

        RegisterPackage(new ResPath("/EngineShaders"), "Robust");
        RegisterPackage(new ResPath("/Shaders"), "Content");
    }

    public unsafe ShaderCompileResultWgsl CompileToWgsl(ResPath path, IReadOnlyDictionary<string, bool> features)
    {
        using var _ = _rwLock.ReadGuard();

        CheckDisposed();

        var resolverOptions = MakeResolverOptions();
        var compileOptions = new WeslCompileOptions
        {
            resolver = &resolverOptions,
            condcomp = 1,
            imports = 1,
            lower = 1,
        };

        var modulePath = ResPathToModulePath(path);

        byte[] modulePathNullTerminated = [.. Encoding.UTF8.GetBytes(modulePath), 0];

        WeslResult result;
        fixed (byte* pPath = modulePathNullTerminated)
        {
            result = Wesl.wesl_compile(null, (sbyte*)pPath, &compileOptions, null, null);
        }

        try
        {
            if (result.success == 0)
            {
                var message = Marshal.PtrToStringUTF8((nint)result.error.message);
                throw new Exception(message);
                return new ShaderCompileResultWgsl([], false);
            }

            var data = MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)result.data);
            return new ShaderCompileResultWgsl(data.ToArray(), true);
        }
        finally
        {
            Wesl.wesl_free_result(&result);
        }
    }

    private unsafe WeslResolverOptions MakeResolverOptions()
    {
        return new WeslResolverOptions
        {
            resolve_source = &ResolveSource,
            resolve_source_free = &ResolveSourceFree,
            userdata = (void*)GCHandle.ToIntPtr(_resolverGCHandle),
        };
    }

    private record struct ShaderModule(ResPath ResourcePath, string ModulePath);

    public void Dispose()
    {
        using var _ = _rwLock.WriteGuard();

        CheckDisposed();

        _disposed = true;

        _resolverGCHandle.Free();
    }

    private void CheckDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ShaderCompiler));
    }

    private Stream? ResolveModulePath(string modulePath)
    {
        var resPath = ModulePathToResPath(modulePath);
        if (!resPath.HasValue)
            return null;

        if (_resourceManager.TryContentFileRead(resPath, out var stream))
            return stream;

        // Try .wgsl as fallback
        resPath = resPath.Value.WithExtension("wgsl");

        return _resourceManager.ContentFileReadOrNull(resPath.Value);
    }

    private ResPath? ModulePathToResPath(string modulePath)
    {
        var components = modulePath.Split("::");
        var packageName = components[0].Split('/')[^1];

        foreach (var package in _packages)
        {
            if (package.Name == packageName)
                return package.BasePath / $"{string.Join('/', components[1..])}.wesl";
        }

        return null;
    }

    private string ResPathToModulePath(ResPath resPath)
    {
        if (resPath.Extension is not ("wesl" or "wgsl"))
            throw new ArgumentException("Shader path must end in .wesl or .wgsl");

        foreach (var package in _packages)
        {
            if (resPath.TryRelativeTo(package.BasePath, out var relative))
            {
                var path = string.Join("::", relative.Value.EnumerateSegments());
                return $"{package.Name}::" + path[..^5]; // Trim .wesl or .wgsl suffix.
            }
        }

        throw new ArgumentException("Shader path must be inside a proper shader package");
    }

    private void RegisterPackage(ResPath root, string packageName)
    {
        _packages.Add(new PackageDefinition
        {
            Name = packageName, BasePath = root
        });
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe WeslResolveSourceResult* ResolveSource(sbyte* modulePath, void* userdata)
    {
        var self = (ShaderCompiler)GCHandle.FromIntPtr((nint)userdata).Target!;
        var path = Marshal.PtrToStringUTF8((nint)modulePath);

        var stream = self.ResolveModulePath(path!);

        var result = (WeslResolveSourceResult*)NativeMemory.Alloc((nuint)sizeof(WeslResolveSourceResult));

        if (stream == null)
        {
            *result = new WeslResolveSourceResult
            {
                success = 0
            };
        }
        else
        {
            var bytes = stream.CopyToArray();

            var nativeSource = (byte*)NativeMemory.Alloc((nuint)bytes.Length + 1);
            bytes.CopyTo(new Span<byte>(nativeSource, bytes.Length));
            nativeSource[bytes.Length] = 0; // Null terminator

            *result = new WeslResolveSourceResult
            {
                success = 1,
                source = (sbyte*)nativeSource,
            };
        }

        return result;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void ResolveSourceFree(WeslResolveSourceResult* result, void* userdata)
    {
        if (result->success != 0)
            NativeMemory.Free(result->source);

        NativeMemory.Free(result);
    }

    private sealed class PackageDefinition
    {
        public required string Name;
        public required ResPath BasePath;
    }
}
