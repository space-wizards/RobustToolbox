using System;
using System.Runtime.InteropServices;
using System.Threading;
using TerraFX.Interop.Windows;

namespace Robust.Shared.Timing;

/// <summary>
/// Helper for more precise sleeping functionality than <see cref="Thread.Sleep(int)"/>.
/// </summary>
internal abstract class PrecisionSleep : IDisposable
{
    /// <summary>
    /// Sleep for the specified amount of time.
    /// </summary>
    public abstract void Sleep(TimeSpan time);

    /// <summary>
    /// Create the most optimal optimization for the current platform.
    /// </summary>
    public static PrecisionSleep Create()
    {
        // Check Windows 10 1803
        if (OperatingSystem.IsWindows() && Environment.OSVersion.Version.Build >= 17134)
            return new PrecisionSleepWindowsHighResolution();

        return new PrecisionSleepUniversal();
    }

    public virtual void Dispose()
    {
    }
}

/// <summary>
/// Universal cross-platform implementation of <see cref="PrecisionSleep"/>. Not very precise!
/// </summary>
internal sealed class PrecisionSleepUniversal : PrecisionSleep
{
    public override void Sleep(TimeSpan time)
    {
        Thread.Sleep(time);
    }
}

/// <summary>
/// High-precision implementation of <see cref="PrecisionSleep"/> that is available since Windows 10 1803.
/// </summary>
internal sealed unsafe class PrecisionSleepWindowsHighResolution : PrecisionSleep
{
    private HANDLE _timerHandle;

    public PrecisionSleepWindowsHighResolution()
    {
        // CREATE_WAITABLE_TIMER_HIGH_RESOLUTION is only supported since Windows 10 1803
        _timerHandle = Windows.CreateWaitableTimerExW(
            null,
            null,
            CREATE.CREATE_WAITABLE_TIMER_HIGH_RESOLUTION,
            Windows.TIMER_ALL_ACCESS);

        if (_timerHandle == HANDLE.NULL)
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
    }

    public override void Sleep(TimeSpan time)
    {
        LARGE_INTEGER due;
        Windows.GetSystemTimeAsFileTime((FILETIME*)(&due));

        due.QuadPart += time.Ticks;

        var success = Windows.SetWaitableTimer(
            _timerHandle,
            &due,
            0,
            null,
            null,
            BOOL.FALSE
        );

        if (!success)
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

        var waitResult = Windows.WaitForSingleObject(_timerHandle, Windows.INFINITE);
        if (waitResult == WAIT.WAIT_FAILED)
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

        GC.KeepAlive(this);
    }

    private void DisposeCore()
    {
        Windows.CloseHandle(_timerHandle);

        _timerHandle = default;
    }

    public override void Dispose()
    {
        DisposeCore();

        GC.SuppressFinalize(this);
    }

    ~PrecisionSleepWindowsHighResolution()
    {
        DisposeCore();
    }
}
