using System;
using System.Runtime.InteropServices;
using System.Security;

namespace SFML
{
    namespace Audio
    {
        ////////////////////////////////////////////////////////////
        /// <summary>
        /// SoundRecorder is an interface for capturing sound data,
        /// it is meant to be used as a base class
        /// </summary>
        ////////////////////////////////////////////////////////////
        public abstract class SoundRecorder : ObjectBase
        {
            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Default constructor
            /// </summary>
            ////////////////////////////////////////////////////////////
            public SoundRecorder() :
                base(IntPtr.Zero)
            {
                myStartCallback   = new StartCallback(OnStart);
                myProcessCallback = new ProcessCallback(ProcessSamples);
                myStopCallback    = new StopCallback(OnStop);

                SetThis(sfSoundRecorder_create(myStartCallback, myProcessCallback, myStopCallback, IntPtr.Zero));
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Start the capture using default sample rate (44100 Hz)
            /// Warning : only one capture can happen at the same time
            /// </summary>
            ////////////////////////////////////////////////////////////
            public void Start()
            {
                Start(44100);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Start the capture.
            /// Warning : only one capture can happen at the same time
            /// </summary>
            /// <param name="sampleRate"> Sound frequency; the more samples, the higher the quality (44100 by default = CD quality)</param>
            ////////////////////////////////////////////////////////////
            public void Start(uint sampleRate)
            {
                sfSoundRecorder_start(CPointer, sampleRate);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Stop the capture
            /// </summary>
            ////////////////////////////////////////////////////////////
            public void Stop()
            {
                sfSoundRecorder_stop(CPointer);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Sample rate of the recorder, in samples per second
            /// </summary>
            ////////////////////////////////////////////////////////////
            public uint SampleRate
            {
                get {return sfSoundRecorder_getSampleRate(CPointer);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Tell if the system supports sound capture.
            /// If not, this class won't be usable
            /// </summary>
            ////////////////////////////////////////////////////////////
            public static bool IsAvailable
            {
                get {return sfSoundRecorder_isAvailable();}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Provide a string describing the object
            /// </summary>
            /// <returns>String description of the object</returns>
            ////////////////////////////////////////////////////////////
            public override string ToString()
            {
                return "[SoundRecorder]" +
                       " SampleRate(" + SampleRate + ")";
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Called when a new capture starts
            /// </summary>
            /// <returns>False to abort recording audio data, true to continue</returns>
            ////////////////////////////////////////////////////////////
            protected virtual bool OnStart()
            {
                // Does nothing by default
                return true;
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Process a new chunk of recorded samples
            /// </summary>
            /// <param name="samples">Array of samples to process</param>
            /// <returns>False to stop recording audio data, true to continue</returns>
            ////////////////////////////////////////////////////////////
            protected abstract bool OnProcessSamples(short[] samples);

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Called when the current capture stops
            /// </summary>
            ////////////////////////////////////////////////////////////
            protected virtual void OnStop()
            {
                // Does nothing by default
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Handle the destruction of the object
            /// </summary>
            /// <param name="disposing">Is the GC disposing the object, or is it an explicit call ?</param>
            ////////////////////////////////////////////////////////////
            protected override void Destroy(bool disposing)
            {
                sfSoundRecorder_destroy(CPointer);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Function called directly by the C library ; convert
            /// arguments and forward them to the internal virtual function
            /// </summary>
            /// <param name="samples">Pointer to the array of samples</param>
            /// <param name="nbSamples">Number of samples in the array</param>
            /// <param name="userData">User data -- unused</param>
            /// <returns>False to stop recording audio data, true to continue</returns>
            ////////////////////////////////////////////////////////////
            private bool ProcessSamples(IntPtr samples, uint nbSamples, IntPtr userData)
            {
                short[] samplesArray = new short[nbSamples];
                Marshal.Copy(samples, samplesArray, 0, samplesArray.Length);

                return OnProcessSamples(samplesArray);
            }

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            private delegate bool StartCallback();

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            private delegate bool ProcessCallback(IntPtr samples, uint nbSamples, IntPtr userData);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            private delegate void StopCallback();

            private StartCallback   myStartCallback;
            private ProcessCallback myProcessCallback;
            private StopCallback    myStopCallback;

            #region Imports
            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntPtr sfSoundRecorder_create(StartCallback OnStart, ProcessCallback OnProcess, StopCallback OnStop, IntPtr UserData);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSoundRecorder_destroy(IntPtr SoundRecorder);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSoundRecorder_start(IntPtr SoundRecorder, uint SampleRate);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSoundRecorder_stop(IntPtr SoundRecorder);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern uint sfSoundRecorder_getSampleRate(IntPtr SoundRecorder);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern bool sfSoundRecorder_isAvailable();
            #endregion
        }
    }
}
