using System;
using System.Runtime.InteropServices;

namespace NFluidsynth
{
    internal static class FluidsynthNative
    {
        [DllImport("libfluidsynth-3", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int fluid_synth_set_gen(IntPtr synth, int chan, int param, float value);

        [DllImport("libfluidsynth-3", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int fluid_player_set_tempo(IntPtr player, int tempoType, double tempo);
    }
}
