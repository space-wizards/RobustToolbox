using System;
using System.Runtime.InteropServices;

namespace ServerServices.Timing
{
    public class TimerQueueTimer : IDisposable
    {

        IntPtr phNewTimer; // Handle to the timer.

        #region Win32 TimerQueueTimer Functions

        [DllImport("kernel32.dll")]
        static extern bool CreateTimerQueueTimer(
            out IntPtr phNewTimer,          // phNewTimer - Pointer to a handle; this is an out value
            IntPtr TimerQueue,              // TimerQueue - Timer queue handle. For the default timer queue, NULL
            WaitOrTimerDelegate Callback,   // Callback - Pointer to the callback function
            IntPtr Parameter,               // Parameter - Value passed to the callback function
            uint DueTime,                   // DueTime - Time (milliseconds), before the timer is set to the signaled state for the first time 
            uint Period,                    // Period - Timer period (milliseconds). If zero, timer is signaled only once
            uint Flags                      // Flags - One or more of the next values (table taken from MSDN):
            // WT_EXECUTEINTIMERTHREAD 	The callback function is invoked by the timer thread itself. This flag should be used only for short tasks or it could affect other timer operations.
            // WT_EXECUTEINIOTHREAD 	The callback function is queued to an I/O worker thread. This flag should be used if the function should be executed in a thread that waits in an alertable state.

                                            // The callback function is queued as an APC. Be sure to address reentrancy issues if the function performs an alertable wait operation.
            // WT_EXECUTEINPERSISTENTTHREAD 	The callback function is queued to a thread that never terminates. This flag should be used only for short tasks or it could affect other timer operations.

                                            // Note that currently no worker thread is persistent, although no worker thread will terminate if there are any pending I/O requests.
            // WT_EXECUTELONGFUNCTION 	Specifies that the callback function can perform a long wait. This flag helps the system to decide if it should create a new thread.
            // WT_EXECUTEONLYONCE 	The timer will be set to the signaled state only once.
            );

        [DllImport("kernel32.dll")]
        static extern bool DeleteTimerQueueTimer(
            IntPtr timerQueue,              // TimerQueue - A handle to the (default) timer queue
            IntPtr timer,                   // Timer - A handle to the timer
            IntPtr completionEvent          // CompletionEvent - A handle to an optional event to be signaled when the function is successful and all callback functions have completed. Can be NULL.
            );

        [DllImport("kernel32.dll")]
        private static extern bool ChangeTimerQueueTimer(
            IntPtr timerQueue,
            IntPtr timer,
            uint dueTime,
            uint period
            );

        [DllImport("kernel32.dll")]
        static extern bool DeleteTimerQueue(IntPtr TimerQueue);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        #endregion

        public delegate void WaitOrTimerDelegate(IntPtr lpParameter, bool timerOrWaitFired);

        public TimerQueueTimer()
        {

        }

        public void Create(uint dueTime, uint period, WaitOrTimerDelegate callbackDelegate)
        {
            IntPtr pParameter = IntPtr.Zero;

            bool success = CreateTimerQueueTimer(
                // Timer handle
                out phNewTimer,
                // Default timer queue. IntPtr.Zero is just a constant value that represents a null pointer.
                IntPtr.Zero,
                // Timer callback function
                callbackDelegate,
                // Callback function parameter
                pParameter,
                // Time (milliseconds), before the timer is set to the signaled state for the first time.
                dueTime,
                // Period - Timer period (milliseconds). If zero, timer is signaled only once.
                period,
                (uint)Flag.WT_EXECUTEINIOTHREAD);

            if (!success)
                throw new QueueTimerException("Error creating QueueTimer");
        }

        public void Delete()
        {
            //bool success = DeleteTimerQueue(IntPtr.Zero);
            bool success = DeleteTimerQueueTimer(
                IntPtr.Zero, // TimerQueue - A handle to the (default) timer queue
                phNewTimer,  // Timer - A handle to the timer
                IntPtr.Zero  // CompletionEvent - A handle to an optional event to be signaled when the function is successful and all callback functions have completed. Can be NULL.
                );
            int error = Marshal.GetLastWin32Error();
            //CloseHandle(phNewTimer);
        }

        public void Change(uint dueTime, uint period)
        {
            bool success = ChangeTimerQueueTimer(
                IntPtr.Zero,
                phNewTimer,
                dueTime,
                period
                );
            int error = Marshal.GetLastWin32Error();
        }

        private enum Flag
        {
            WT_EXECUTEDEFAULT = 0x00000000,
            WT_EXECUTEINIOTHREAD = 0x00000001,
            //WT_EXECUTEINWAITTHREAD       = 0x00000004,
            WT_EXECUTEONLYONCE = 0x00000008,
            WT_EXECUTELONGFUNCTION = 0x00000010,
            WT_EXECUTEINTIMERTHREAD = 0x00000020,
            WT_EXECUTEINPERSISTENTTHREAD = 0x00000080,
            //WT_TRANSFER_IMPERSONATION    = 0x00000100
        }

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            Delete();
        }

        #endregion
    }

    public class QueueTimerException : Exception
    {
        public QueueTimerException(string message)
            : base(message)
        {
        }

        public QueueTimerException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
