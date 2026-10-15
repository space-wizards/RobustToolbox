using System;
using bottlenoselabs.C2CS.Runtime;
using Robust.Shared.Maths;
using static Tracy.PInvoke;

namespace Robust.Shared.Profiling;

// Tracy-side partial ProfManager

public sealed partial class ProfManager
{
    /// <summary>
    /// Proxy to <c>prof.tracy.enabled</c> CVar.
    /// </summary>
    public bool IsTracyEnabled { get; private set; }

    partial void InitializeTracyCvars()
    {
        _cfg.OnValueChanged(CVars.TracyProfEnabled, b => IsTracyEnabled = b, true);
    }

    private bool _tracyPlotsInitialized;
    private long _tracyLastAllocatedBytes;
    private CString _tracyPlotManagedHeap;
    private CString _tracyPlotAllocRate;
    private CString _tracyPlotGen0;
    private CString _tracyPlotGen1;
    private CString _tracyPlotGen2;
    private CString _tracyEntityCount;

    internal unsafe void EmitFrameImage(void* image, ushort width, ushort height, byte offset, bool flip)
    {
        if (!IsTracyEnabled)
            return;

        TracyEmitFrameImage(image, width, height, offset, flip ? 1 : 0);
    }

    internal void EmitMemoryPlots(int gcGen0, int gcGen1, int gcGen2)
    {
        if (!IsTracyEnabled)
            return;

        if (!_tracyPlotsInitialized)
            InitTracyPlots();

        var allocated = GC.GetTotalAllocatedBytes();

        TracyEmitPlotInt(_tracyPlotManagedHeap, GC.GetTotalMemory(false));
        TracyEmitPlotInt(_tracyPlotAllocRate, allocated - _tracyLastAllocatedBytes);
        TracyEmitPlotInt(_tracyPlotGen0, gcGen0);
        TracyEmitPlotInt(_tracyPlotGen1, gcGen1);
        TracyEmitPlotInt(_tracyPlotGen2, gcGen2);

        _tracyLastAllocatedBytes = allocated;

    }

    internal void EmitEntities(int entityCount)
    {
        if (!IsTracyEnabled)
            return;

        if (!_tracyPlotsInitialized)
            InitTracyPlots();

        TracyEmitPlotInt(_tracyEntityCount, entityCount);
    }

    private void InitTracyPlots()
    {
        _tracyPlotsInitialized = true;
        _tracyLastAllocatedBytes = GC.GetTotalAllocatedBytes();

        _tracyPlotManagedHeap = CString.FromString("Managed Heap");
        _tracyPlotAllocRate = CString.FromString("Alloc Rate");
        _tracyPlotGen0 = CString.FromString("GC Gen 0");
        _tracyPlotGen1 = CString.FromString("GC Gen 1");
        _tracyPlotGen2 = CString.FromString("GC Gen 2");
        _tracyEntityCount = CString.FromString("Entities");

        const int memoryFormat = (int)TracyPlotFormatEnum.TracyPlotFormatMemory;
        TracyEmitPlotConfig(_tracyPlotManagedHeap, memoryFormat, step: 0, fill: 1, color: 0);
        TracyEmitPlotConfig(_tracyPlotAllocRate, memoryFormat, step: 0, fill: 1, color: 0);

        const int numberFormat = (int)TracyPlotFormatEnum.TracyPlotFormatNumber;
        TracyEmitPlotConfig(_tracyPlotGen0, numberFormat, step: 1, fill: 0, color: 0);
        TracyEmitPlotConfig(_tracyPlotGen1, numberFormat, step: 1, fill: 0, color: 0);
        TracyEmitPlotConfig(_tracyPlotGen2, numberFormat, step: 1, fill: 0, color: 0);
        TracyEmitPlotConfig(_tracyEntityCount, numberFormat, step: 1, fill: 0, color: 0);
    }

    /// <summary>
    /// Marks the boundary of a continuous Tracy frame. Used by <see cref="FrameGuard"/>.
    /// </summary>
    private static void EmitFrameMark()
    {
        TracyEmitFrameMark(null);
    }

    /// <summary>
    /// Creates a <seealso cref="CString"/> for use by Tracy. Also returns the
    /// length of the string for interop convenience.
    /// </summary>
    internal static CString GetCString(string? fromString, out ulong cLength)
    {
        if (fromString == null)
        {
            cLength = 0;
            return new CString(0);
        }

        cLength = (ulong)fromString.Length;
        return CString.FromString(fromString);
    }

    private static TracyProfilerZone BeginTracyZone(string name, int lineNumber, Color? color, string? filePath, string? memberName)
    {
        using var fileStr = GetCString(filePath, out var fileLn);
        using var memberStr = GetCString(memberName, out var memberLn);
        using var nameStr = GetCString(name, out var nameLn);
        var srcLocId = TracyAllocSrclocName((uint)lineNumber, fileStr, fileLn, memberStr, memberLn, nameStr, nameLn, (uint) (color?.ToArgb() ?? 0));
        var context = TracyEmitZoneBeginAlloc(srcLocId, 1);
        return new TracyProfilerZone(context);
    }
}

internal readonly struct TracyProfilerZone : IDisposable
{
    private readonly TracyCZoneCtx _context;

    private uint Id => _context.Data.Id;

    private int Active => _context.Data.Active;

    internal TracyProfilerZone(TracyCZoneCtx context)
    {
        _context = context;
    }

    internal void EmitText(string text)
    {
        using var textStr = ProfManager.GetCString(text, out var textLn);
        TracyEmitZoneText(_context, textStr, textLn);
    }

    public void Dispose()
    {
        TracyEmitZoneEnd(_context);
    }
}
