using System;
using System.Runtime.InteropServices;
using System.Security;

namespace SFML
{
    namespace Audio
    {
        ////////////////////////////////////////////////////////////
        /// <summary>
        /// SoundStream is a streamed sound, ie. samples are acquired
        /// while the sound is playing. Use it for big sounds that would
        /// require hundreds of MB in memory (see Music),
        /// or for streaming sound from the network
        /// </summary>
        ////////////////////////////////////////////////////////////
        public abstract class SoundStream : ObjectBase
        {
            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Default constructor
            /// </summary>
            ////////////////////////////////////////////////////////////
            public SoundStream() :
                base(IntPtr.Zero)
            {
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Play the sound stream
            /// </summary>
            ////////////////////////////////////////////////////////////
            public void Play()
            {
                sfSoundStream_play(CPointer);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Pause the sound stream
            /// </summary>
            ////////////////////////////////////////////////////////////
            public void Pause()
            {
                sfSoundStream_pause(CPointer);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Stop the sound stream
            /// </summary>
            ////////////////////////////////////////////////////////////
            public void Stop()
            {
                sfSoundStream_stop(CPointer);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Samples rate, in samples per second
            /// </summary>
            ////////////////////////////////////////////////////////////
            public uint SampleRate
            {
                get {return sfSoundStream_getSampleRate(CPointer);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Number of channels (1 = mono, 2 = stereo)
            /// </summary>
            ////////////////////////////////////////////////////////////
            public uint ChannelCount
            {
                get {return sfSoundStream_getChannelCount(CPointer);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Current status of the sound stream (see SoundStatus enum)
            /// </summary>
            ////////////////////////////////////////////////////////////
            public SoundStatus Status
            {
                get {return sfSoundStream_getStatus(CPointer);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Loop state of the sound stream. Default value is false
            /// </summary>
            ////////////////////////////////////////////////////////////
            public bool Loop
            {
                get {return sfSoundStream_getLoop(CPointer);}
                set {sfSoundStream_setLoop(CPointer, value);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Pitch of the sound stream. Default value is 1
            /// </summary>
            ////////////////////////////////////////////////////////////
            public float Pitch
            {
                get {return sfSoundStream_getPitch(CPointer);}
                set {sfSoundStream_setPitch(CPointer, value);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Volume of the sound stream, in range [0, 100]. Default value is 100
            /// </summary>
            ////////////////////////////////////////////////////////////
            public float Volume
            {
                get {return sfSoundStream_getVolume(CPointer);}
                set {sfSoundStream_setVolume(CPointer, value);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// 3D position of the sound stream. Default value is (0, 0, 0)
            /// </summary>
            ////////////////////////////////////////////////////////////
            public Vector3f Position
            {
                get {return sfSoundStream_getPosition(CPointer);}
                set {sfSoundStream_setPosition(CPointer, value);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Is the sound stream's position relative to the listener's position,
            /// or is it absolute?
            /// Default value is false (absolute)
            /// </summary>
            ////////////////////////////////////////////////////////////
            public bool RelativeToListener
            {
                get {return sfSoundStream_isRelativeToListener(CPointer);}
                set {sfSoundStream_setRelativeToListener(CPointer, value);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Minimum distance of the sound stream. Closer than this distance,
            /// the listener will hear the sound at its maximum volume.
            /// The default value is 1
            /// </summary>
            ////////////////////////////////////////////////////////////
            public float MinDistance
            {
                get {return sfSoundStream_getMinDistance(CPointer);}
                set {sfSoundStream_setMinDistance(CPointer, value);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Attenuation factor. The higher the attenuation, the
            /// more the sound will be attenuated with distance from listener.
            /// The default value is 1
            /// </summary>
            ////////////////////////////////////////////////////////////
            public float Attenuation
            {
                get {return sfSoundStream_getAttenuation(CPointer);}
                set {sfSoundStream_setAttenuation(CPointer, value);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Current playing position
            /// </summary>
            ////////////////////////////////////////////////////////////
            public TimeSpan PlayingOffset
            {
                get
                {
                    long microseconds = sfSoundStream_getPlayingOffset(CPointer);
                    return TimeSpan.FromTicks(microseconds * TimeSpan.TicksPerMillisecond / 1000);
                }
                set
                {
                    long microseconds = value.Ticks / (TimeSpan.TicksPerMillisecond / 1000);
                    sfSoundStream_setPlayingOffset(CPointer, microseconds);
                }
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Provide a string describing the object
            /// </summary>
            /// <returns>String description of the object</returns>
            ////////////////////////////////////////////////////////////
            public override string ToString()
            {
                return "[SoundStream]" +
                       " SampleRate(" + SampleRate + ")" +
                       " ChannelCount(" + ChannelCount + ")" +
                       " Status(" + Status + ")" +
                       " Loop(" + Loop + ")" +
                       " Pitch(" + Pitch + ")" +
                       " Volume(" + Volume + ")" +
                       " Position(" + Position + ")" +
                       " RelativeToListener(" + RelativeToListener + ")" +
                       " MinDistance(" + MinDistance + ")" +
                       " Attenuation(" + Attenuation + ")" +
                       " PlayingOffset(" + PlayingOffset + ")";
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Set the audio stream parameters, you must call it before Play()
            /// </summary>
            /// <param name="channelCount">Number of channels</param>
            /// <param name="sampleRate">Sample rate, in samples per second</param>
            ////////////////////////////////////////////////////////////
            protected void Initialize(uint channelCount, uint sampleRate)
            {
                myGetDataCallback = new GetDataCallbackType(GetData);
                mySeekCallback    = new SeekCallbackType(Seek);
                SetThis(sfSoundStream_create(myGetDataCallback, mySeekCallback, channelCount, sampleRate, IntPtr.Zero));
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Virtual function called each time new audio data is needed to feed the stream
            /// </summary>
            /// <param name="samples">Array of samples to fill for the stream</param>
            /// <returns>True to continue playback, false to stop</returns>
            ////////////////////////////////////////////////////////////
            protected abstract bool OnGetData(out short[] samples);

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Virtual function called to seek into the stream
            /// </summary>
            /// <param name="timeOffset">New position</param>
            ////////////////////////////////////////////////////////////
            protected abstract void OnSeek(TimeSpan timeOffset);

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Handle the destruction of the object
            /// </summary>
            /// <param name="disposing">Is the GC disposing the object, or is it an explicit call ?</param>
            ////////////////////////////////////////////////////////////
            protected override void Destroy(bool disposing)
            {
                sfSoundStream_destroy(CPointer);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Structure mapping the C library arguments passed to the data callback
            /// </summary>
            ////////////////////////////////////////////////////////////
            [StructLayout(LayoutKind.Sequential)]
            private struct Chunk
            {
                unsafe public short* samples;
                public uint          sampleCount;
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Called each time new audio data is needed to feed the stream
            /// </summary>
            /// <param name="dataChunk">Data chunk to fill with new audio samples</param>
            /// <param name="userData">User data -- unused</param>
            /// <returns>True to continue playback, false to stop</returns>
            ////////////////////////////////////////////////////////////
            private bool GetData(ref Chunk dataChunk, IntPtr userData)
            {
                if (OnGetData(out myTempBuffer))
                {
                    unsafe
                    {
                        fixed (short* samplesPtr = myTempBuffer)
                        {
                            dataChunk.samples = samplesPtr;
                            dataChunk.sampleCount = (uint)myTempBuffer.Length;
                        }
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Called to seek in the stream
            /// </summary>
            /// <param name="timeOffset">New position</param>
            /// <param name="userData">User data -- unused</param>
            /// <returns>If false is returned, the playback is aborted</returns>
            ////////////////////////////////////////////////////////////
            private void Seek(long timeOffset, IntPtr userData)
            {
                OnSeek(TimeSpan.FromTicks(timeOffset * TimeSpan.TicksPerMillisecond / 1000));
            }

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            private delegate bool GetDataCallbackType(ref Chunk dataChunk, IntPtr UserData);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            private delegate void SeekCallbackType(long timeOffset, IntPtr UserData);

            private GetDataCallbackType myGetDataCallback;
            private SeekCallbackType    mySeekCallback;
            private short[]             myTempBuffer;

            #region Imports

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntPtr sfSoundStream_create(GetDataCallbackType OnGetData, SeekCallbackType OnSeek, uint ChannelCount, uint SampleRate, IntPtr UserData);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSoundStream_destroy(IntPtr SoundStreamStream);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSoundStream_play(IntPtr SoundStream);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSoundStream_pause(IntPtr SoundStream);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSoundStream_stop(IntPtr SoundStream);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern SoundStatus sfSoundStream_getStatus(IntPtr SoundStream);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern uint sfSoundStream_getChannelCount(IntPtr SoundStream);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern uint sfSoundStream_getSampleRate(IntPtr SoundStream);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSoundStream_setLoop(IntPtr SoundStream, bool Loop);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSoundStream_setPitch(IntPtr SoundStream, float Pitch);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSoundStream_setVolume(IntPtr SoundStream, float Volume);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSoundStream_setPosition(IntPtr SoundStream, Vector3f position);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSoundStream_setRelativeToListener(IntPtr SoundStream, bool Relative);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSoundStream_setMinDistance(IntPtr SoundStream, float MinDistance);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSoundStream_setAttenuation(IntPtr SoundStream, float Attenuation);
            
            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSoundStream_setPlayingOffset(IntPtr SoundStream, long TimeOffset);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern bool sfSoundStream_getLoop(IntPtr SoundStream);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern float sfSoundStream_getPitch(IntPtr SoundStream);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern float sfSoundStream_getVolume(IntPtr SoundStream);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern Vector3f sfSoundStream_getPosition(IntPtr SoundStream);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern bool sfSoundStream_isRelativeToListener(IntPtr SoundStream);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern float sfSoundStream_getMinDistance(IntPtr SoundStream);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern float sfSoundStream_getAttenuation(IntPtr SoundStream);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern long sfSoundStream_getPlayingOffset(IntPtr SoundStream);

            #endregion
        }
    }
}