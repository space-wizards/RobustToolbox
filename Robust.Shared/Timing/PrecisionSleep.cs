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

        if (OperatingSystem.IsLinux())
            return new PrecisionSleepLinuxNanosleep();

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
            Marshal.ThrowExceptionForHR(Windows.HRESULT_FROM_WIN32(Marshal.GetLastSystemError()));
    }

    public override void Sleep(TimeSpan time)
    {
        LARGE_INTEGER due;
        // negative = relative time.
        due.QuadPart = -time.Ticks;

        var success = Windows.SetWaitableTimer(
            _timerHandle,
            &due,
            0,
            null,
            null,
            BOOL.FALSE
        );

        if (!success)
            Marshal.ThrowExceptionForHR(Windows.HRESULT_FROM_WIN32(Marshal.GetLastSystemError()));

        var waitResult = Windows.WaitForSingleObject(_timerHandle, Windows.INFINITE);
        if (waitResult == WAIT.WAIT_FAILED)
            Marshal.ThrowExceptionForHR(Windows.HRESULT_FROM_WIN32(Marshal.GetLastSystemError()));

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

/// <summary>
/// High-precision implementation of <see cref="PrecisionSleep"/> that is available on Linux.
/// </summary>
internal sealed unsafe class PrecisionSleepLinuxNanosleep : PrecisionSleep
{
    public override void Sleep(TimeSpan time)
    {
        timespec timeSpec;
        timeSpec.tv_sec = Math.DivRem(time.Ticks, TimeSpan.TicksPerSecond, out var ticksRem);
        timeSpec.tv_nsec = ticksRem * TimeSpan.NanosecondsPerTick;

        while (true)
        {
            timespec rem;
            var result = nanosleep(&timeSpec, &rem);
            if (result == 0)
                return;

            var error = Marshal.GetLastSystemError();
            if (error != 4) // EINTR
                throw new Exception($"nanosleep failed: {error}");

            timeSpec = rem;
        }
    }

#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    // ReSharper disable IdentifierTypo
    // ReSharper disable InconsistentNaming
    [DllImport("libc.so.6", SetLastError=true)]
    private static extern int nanosleep(timespec* req, timespec* rem);

    private struct timespec
    {
        public long tv_sec;
        public long tv_nsec;
    }

    private struct timeval
    {
        public long tv_sec;
        public long tv_usec;
    }
    // ReSharper restore InconsistentNaming
    // ReSharper restore IdentifierTypo
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
#pragma warning restore CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
}
