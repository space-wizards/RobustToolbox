using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace SS13_Shared.Minidump
{
    public static class MiniDump
    {
        [Flags]
        public enum MINIDUMP_TYPE
        {
            MiniDumpNormal = 0x00000000,
            MiniDumpWithDataSegs = 0x00000001,
            MiniDumpWithFullMemory = 0x00000002,
            MiniDumpWithHandleData = 0x00000004,
            MiniDumpFilterMemory = 0x00000008,
            MiniDumpScanMemory = 0x00000010,
            MiniDumpWithUnloadedModules = 0x00000020,
            MiniDumpWithIndirectlyReferencedMemory = 0x00000040,
            MiniDumpFilterModulePaths = 0x00000080,
            MiniDumpWithProcessThreadData = 0x00000100,
            MiniDumpWithPrivateReadWriteMemory = 0x00000200,
            MiniDumpWithoutOptionalData = 0x00000400,
            MiniDumpWithFullMemoryInfo = 0x00000800,
            MiniDumpWithThreadInfo = 0x00001000,
            MiniDumpWithCodeSegs = 0x00002000,
            MiniDumpWithoutManagedState = 0x00004000,
        };
        
        [DllImport("clrdump.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CreateDump(Int32 processId, string fileName,
            Int32 dumpType, Int32 excThreadId, IntPtr extPtrs);
        [DllImport("clrdump.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool RegisterFilter(string fileName, Int32 dumpType);
        [DllImport("clrdump.dll", SetLastError = true)]
        static extern bool UnregisterFilter();
        [DllImport("clrdump.dll")]
        static extern Int32 SetFilterOptions(Int32 options);

        public static bool Register(string fileName, MINIDUMP_TYPE options)
        {
            return RegisterFilter(fileName, (Int32)options);
        }

        public static bool Write(string fileName, MINIDUMP_TYPE options, int? threadId = null)
        {
            var process = Process.GetCurrentProcess();
            var processId = process.Id;
            int realThreadId;
            if (threadId == null)
                realThreadId = Thread.CurrentThread.ManagedThreadId;
            else
                realThreadId = (int)threadId;
            
            return CreateDump(processId, fileName, (Int32) options, realThreadId,
                       Marshal.GetExceptionPointers());
        }
    }
}
