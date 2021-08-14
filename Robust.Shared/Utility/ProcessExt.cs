using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo
// ReSharper disable CommentTypo
#pragma warning disable 649

namespace Robust.Shared.Utility
{
    internal static unsafe class ProcessExt
    {
        // THIS ENTIRE METHOD EXISTS PURELY OUT OF SPITE FOR MICROSOFT.
        // NOT TO SAVE 0.3% CPU USAGE ON WINDOWS. NAH.
        // TO SPITE MICROSOFT.
        // WHAT ABOUT THE 2.2% CPU USAGE IN CONSOLE.KEYAVAILABLE? WHAT ABOUT THAT WHATABOUTISM HUH???
        // I WANTED TO GO TO BED NOW IT'S 4:20 AM (BASED) GOD DAMNIT.
        // AS A RESULT, EVERYTHING HERE WILL BE WRITTEN IN *FUCKING* CAPS.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static long GetPrivateMemorySize64NotSlowHolyFuckingShitMicrosoft(this Process process)
        {
            if (!OperatingSystem.IsWindows())
                // IT'S NOT SLOW ON LINUX, *LUCKILY*.
                // WELL I DIDN'T PROFILE IT BUT THEY DON'T DO A STUPID SEARCH OVER EVERY PROCESS.
                // IT USES PROCFS, IF YOU'RE CURIOUS.
                return process.PrivateMemorySize64;

            //
            // GLASS TOLD ME TO PUT A TL;DR IN SO TL;DR
            // PROCESS.PRIVATEMEMORYSIZE64 (AND A BUNCH OF OTHERS) ARE SLOW AS BALLS (2+ms for me).
            // THIS ISN'T.
            //
            // AS IT *FUCKING* TURNS OUT.
            // *MANY* OF THE FIELDS ON PROCESS (THE CLASS, YOU KNOW THE ONE THIS METHOD IS AN EXTENSION FOR) HAVE TO FETCH A "PROCESS INFO".
            // HOW DO YOU FETCH A PROCESS INFO ON WINDOWS? OH YEAH YOU CALL NTQUERYSYSTEMINFORMATION APPARENTLY.
            // OH WAIT. THAT RETURNS INFO FOR *EVERY* PROCESS ON THE FUCKING SYSTEM.
            // SO YES. IF YOU LOOK UP PROCESS.PRIVATEMEMORYSIZE64, IT HAS TO SEARCH THROUGH *EVERY PROCESS ON THE FUCKING SYSTEM* TO FIND THE PROCESS IT'S LOOKING FOR.
            // THIS TAKES MORE THAN 2 FUCKING MILLISECONDS PER LOOKUP ON MY SYSTEM.
            // IF THE TITLE BAR ON THE CONSOLE WAS UPDATED EVERY TICK INSTEAD OF ONCE PER FRAME, THIS WOULD LITERALLY BE MOST OF THE IDLE SERVER CPU USAGE.
            // ***ARE YOU FUCKING KIDDING ME ????***
            // OH YEAH, NTQUERYSYSTEMINFORMATION IS DOCUMENTED AS AN UNSTABLE API THAT CAN CHANGE AT ANY TIME, APPARENTLY.
            // .NET FEELS IT'S FINE TO USE THAT (YEAH GUESS THEY'RE NEVER REMOVING IT FROM WINDOWS LMAO)
            // ARE YOU FUCKING TELLING ME THE .NET TEAM COULDN'T HAVE WALKED TO THE OTHER SIDE OF THE OFFICE AND TOLD THE KERNEL TEAM TO ADD A VERSION OF NTQUERYSYSTEMINFORMATION THAT FETCHES INFO FOR A SINGLE PROCESS?
            // THEY THOUGHT THIS SHIT WAS FUCKING ACCEPTABLE?
            // JESUS FUCKING CHRIST.
            // ANYWAYS, THIS EXTENSION METHOD USES GETPROCESSMEMORYINFO() INSTEAD BECAUSE THAT'S PROBABLY NOT FUCKING MORONIC.
            //

            PROCESS_MEMORY_COUNTERS_EX counters;

            if (GetProcessMemoryInfo(process.Handle, &counters, sizeof(PROCESS_MEMORY_COUNTERS_EX)) == 0)
                return 0;

            var count = counters.PrivateUsage;

            return count;
        }

        private struct PROCESS_MEMORY_COUNTERS_EX
        {
            public int cb;
            public int PageFaultCount;
            public nint PeakWorkingSetSize;
            public nint WorkingSetSize;
            public nint QuotaPeakPagedPoolUsage;
            public nint QuotaPagedPoolUsage;
            public nint QuotaPeakNonPagedPoolUsage;
            public nint QuotaNonPagedPoolUsage;
            public nint PagefileUsage;
            public nint PeakPagefileUsage;
            public nint PrivateUsage;
        }

        // ReSharper disable once StringLiteralTypo
        [DllImport("psapi.dll", SetLastError = true)]
        private static extern int GetProcessMemoryInfo(
            IntPtr Process,
            PROCESS_MEMORY_COUNTERS_EX* ppsmemCounters,
            int cb);
    }
}
