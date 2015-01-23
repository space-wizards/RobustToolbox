using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace SS14.Server.Timing
{
    public enum TimerQueueTimerFlags : uint
    {
        ExecuteDefault = 0x0000,
        ExecuteInTimerThread = 0x0020,
        ExecuteInIoThread = 0x0001,
        ExecuteInPersistentThread = 0x0080,
        ExecuteLongFunction = 0x0010,
        ExecuteOnlyOnce = 0x0008,
        TransferImpersonation = 0x0100,
    }
    public delegate void Win32WaitOrTimerCallback(
        IntPtr lpParam,
        [MarshalAs(UnmanagedType.U1)]bool bTimedOut);
    static public class TQTimerWin32
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public extern static IntPtr CreateTimerQueue();
        [DllImport("kernel32.dll", SetLastError = true)]
        public extern static bool DeleteTimerQueue(IntPtr timerQueue);
        [DllImport("kernel32.dll", SetLastError = true)]
        public extern static bool DeleteTimerQueueEx(IntPtr timerQueue, IntPtr completionEvent);
        [DllImport("kernel32.dll", SetLastError = true)]
        public extern static bool CreateTimerQueueTimer(
            out IntPtr newTimer,
            IntPtr timerQueue,
            Win32WaitOrTimerCallback callback,
            IntPtr userState,
            uint dueTime,
            uint period,
            TimerQueueTimerFlags flags);
        [DllImport("kernel32.dll", SetLastError = true)]
        public extern static bool ChangeTimerQueueTimer(
            IntPtr timerQueue,
            ref IntPtr timer,
            uint dueTime,
            uint period);
        [DllImport("kernel32.dll", SetLastError = true)]
        public extern static bool DeleteTimerQueueTimer(
            IntPtr timerQueue,
            IntPtr timer,
            IntPtr completionEvent);
    }
    public class TimerQueue : IDisposable,IMainLoopTimer
    {
        public IntPtr Handle { get; private set; }
        public static TimerQueue Default { get; private set; }
        static TimerQueue()
        {
            Default = new TimerQueue(IntPtr.Zero);
        }
        private TimerQueue(IntPtr handle)
        {
            Handle = handle;
        }
        public TimerQueue()
        {
            Handle = TQTimerWin32.CreateTimerQueue();
            if (Handle == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Error creating timer queue.");
            }
        }
        ~TimerQueue()
        {
            Dispose(false);
        }
        public TimerQueueTimer CreateTimer(
            TimerCallback callback,
            object state,
            uint dueTime,
            uint period)
        {
            return CreateTimer(callback, state, dueTime, period, TimerQueueTimerFlags.ExecuteDefault);
        }
        public TimerQueueTimer CreateTimer(
            TimerCallback callback,
            object state,
            uint dueTime,
            uint period,
            TimerQueueTimerFlags flags)
        {
            return new TimerQueueTimer(this, callback, state, dueTime, period, flags);
        }
        public TimerQueueTimer CreateOneShot(
            TimerCallback callback,
            object state,
            uint dueTime)
        {
            return CreateOneShot(callback, state, dueTime, TimerQueueTimerFlags.ExecuteDefault);
        }
        public TimerQueueTimer CreateOneShot(
            TimerCallback callback,
            object state,
            uint dueTime,
            TimerQueueTimerFlags flags)
        {
            return CreateTimer(callback, state, dueTime, 0, (flags | TimerQueueTimerFlags.ExecuteOnlyOnce));
        }
        private IntPtr CompletionEventHandle = new IntPtr(-1);
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        public void Dispose(WaitHandle completionEvent)
        {
            CompletionEventHandle = completionEvent.SafeWaitHandle.DangerousGetHandle();
            Dispose();
        }
        private bool Disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (Handle != IntPtr.Zero)
                {
                    bool rslt = TQTimerWin32.DeleteTimerQueueEx(Handle, CompletionEventHandle);
                    if (!rslt)
                    {
                        int err = Marshal.GetLastWin32Error();
                        throw new Win32Exception(err, "Error disposing timer queue");
                    }
                }
                Disposed = true;
            }
        }
        public Object CreateMainLoopTimer(MainServerLoop mainLoop, uint period)
        {
            return CreateTimer((s) => {mainLoop();}, null, 0, period);
        }
    }
    public class TimerQueueTimer : IDisposable
    {
        private TimerQueue MyQueue;
        private TimerCallback Callback;
        private Win32WaitOrTimerCallback win32WaitOrTimerCallback;
        private object UserState;
        private IntPtr Handle;
        internal TimerQueueTimer(
            TimerQueue queue,
            TimerCallback cb,
            object state,
            uint dueTime,
            uint period,
            TimerQueueTimerFlags flags)
        {
            MyQueue = queue;
            Callback = cb;
            win32WaitOrTimerCallback = TimerCallback;
            UserState = state;
            bool rslt = TQTimerWin32.CreateTimerQueueTimer(
                out Handle,
                MyQueue.Handle,
                win32WaitOrTimerCallback,
                IntPtr.Zero,
                dueTime,
                period,
                flags);
            if (!rslt)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Error creating timer.");
            }
        }
        ~TimerQueueTimer()
        {
            Dispose(false);
        }
        public void Change(uint dueTime, uint period)
        {
            bool rslt = TQTimerWin32.ChangeTimerQueueTimer(MyQueue.Handle, ref Handle, dueTime, period);
            if (!rslt)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Error changing timer.");
            }
        }
        private void TimerCallback(IntPtr state, bool bExpired)
        {
            Callback(UserState);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private IntPtr completionEventHandle = new IntPtr(-1);
        public void Dispose(WaitHandle completionEvent)
        {
            completionEventHandle = completionEvent.SafeWaitHandle.DangerousGetHandle();
            this.Dispose();
        }
        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                bool rslt = TQTimerWin32.DeleteTimerQueueTimer(MyQueue.Handle,
                    Handle, completionEventHandle);
                if (!rslt)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Error deleting timer.");
                }
                disposed = true;
            }
        }
    }
}
