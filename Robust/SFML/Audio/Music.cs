using System;
using System.Runtime.InteropServices;
using System.Security;
using System.IO;
using SFML.Window;

namespace SFML
{
    namespace Audio
    {
        ////////////////////////////////////////////////////////////
        /// <summary>
        /// Music defines a big sound played using streaming,
        /// so usually what we call a music :)
        /// </summary>
        ////////////////////////////////////////////////////////////
        public class Music : ObjectBase
        {
            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct the music from a file
            /// </summary>
            /// <param name="filename">Path of the music file to load</param>
            ////////////////////////////////////////////////////////////
            public Music(string filename) :
                base(sfMusic_createFromFile(filename))
            {
                if (CPointer == IntPtr.Zero)
                    throw new LoadingFailedException("music", filename);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct the music from a custom stream
            /// </summary>
            /// <param name="stream">Source stream to read from</param>
            ////////////////////////////////////////////////////////////
            public Music(Stream stream) :
                base(IntPtr.Zero)
            {
                myStream = new StreamAdaptor(stream);
                SetThis(sfMusic_createFromStream(myStream.InputStreamPtr));

                if (CPointer == IntPtr.Zero)
                    throw new LoadingFailedException("music");
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Play the music
            /// </summary>
            ////////////////////////////////////////////////////////////
            public void Play()
            {
                sfMusic_play(CPointer);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Pause the music
            /// </summary>
            ////////////////////////////////////////////////////////////
            public void Pause()
            {
                sfMusic_pause(CPointer);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Stop the music
            /// </summary>
            ////////////////////////////////////////////////////////////
            public void Stop()
            {
                sfMusic_stop(CPointer);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Samples rate, in samples per second
            /// </summary>
            ////////////////////////////////////////////////////////////
            public uint SampleRate
            {
                get {return sfMusic_getSampleRate(CPointer);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Number of channels (1 = mono, 2 = stereo)
            /// </summary>
            ////////////////////////////////////////////////////////////
            public uint ChannelCount
            {
                get {return sfMusic_getChannelCount(CPointer);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Current status of the music (see SoundStatus enum)
            /// </summary>
            ////////////////////////////////////////////////////////////
            public SoundStatus Status
            {
                get {return sfMusic_getStatus(CPointer);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Total duration of the music
            /// </summary>
            ////////////////////////////////////////////////////////////
            public TimeSpan Duration
            {
                get
                {
                    long microseconds = sfMusic_getDuration(CPointer);
                    return TimeSpan.FromTicks(microseconds * TimeSpan.TicksPerMillisecond / 1000);
                }
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Loop state of the sound. Default value is false
            /// </summary>
            ////////////////////////////////////////////////////////////
            public bool Loop
            {
                get {return sfMusic_getLoop(CPointer);}
                set {sfMusic_setLoop(CPointer, value);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Pitch of the music. Default value is 1
            /// </summary>
            ////////////////////////////////////////////////////////////
            public float Pitch
            {
                get {return sfMusic_getPitch(CPointer);}
                set {sfMusic_setPitch(CPointer, value);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Volume of the music, in range [0, 100]. Default value is 100
            /// </summary>
            ////////////////////////////////////////////////////////////
            public float Volume
            {
                get {return sfMusic_getVolume(CPointer);}
                set {sfMusic_setVolume(CPointer, value);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// 3D position of the music. Default value is (0, 0, 0)
            /// </summary>
            ////////////////////////////////////////////////////////////
            public Vector3f Position
            {
                get {return sfMusic_getPosition(CPointer);;}
                set {sfMusic_setPosition(CPointer, value);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Is the music's position relative to the listener's position,
            /// or is it absolute?
            /// Default value is false (absolute)
            /// </summary>
            ////////////////////////////////////////////////////////////
            public bool RelativeToListener
            {
                get {return sfMusic_isRelativeToListener(CPointer);}
                set {sfMusic_setRelativeToListener(CPointer, value);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Minimum distance of the music. Closer than this distance,
            /// the listener will hear the sound at its maximum volume.
            /// The default value is 1
            /// </summary>
            ////////////////////////////////////////////////////////////
            public float MinDistance
            {
                get {return sfMusic_getMinDistance(CPointer);}
                set {sfMusic_setMinDistance(CPointer, value);}
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
                get {return sfMusic_getAttenuation(CPointer);}
                set {sfMusic_setAttenuation(CPointer, value);}
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
                    long microseconds = sfMusic_getPlayingOffset(CPointer);
                    return TimeSpan.FromTicks(microseconds * TimeSpan.TicksPerMillisecond / 1000);
                }
                set
                {
                    long microseconds = value.Ticks / (TimeSpan.TicksPerMillisecond / 1000);
                    sfMusic_setPlayingOffset(CPointer, microseconds);
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
                return "[Music]" +
                       " SampleRate(" + SampleRate + ")" +
                       " ChannelCount(" + ChannelCount + ")" +
                       " Status(" + Status + ")" +
                       " Duration(" + Duration + ")" +
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
            /// Handle the destruction of the object
            /// </summary>
            /// <param name="disposing">Is the GC disposing the object, or is it an explicit call ?</param>
            ////////////////////////////////////////////////////////////
            protected override void Destroy(bool disposing)
            {
                if (disposing)
                {
                    if (myStream != null)
                        myStream.Dispose();
                }

                sfMusic_destroy(CPointer);
            }

            private StreamAdaptor myStream = null;

            #region Imports
            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntPtr sfMusic_createFromFile(string Filename);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            unsafe static extern IntPtr sfMusic_createFromStream(IntPtr stream);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfMusic_destroy(IntPtr MusicStream);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfMusic_play(IntPtr Music);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfMusic_pause(IntPtr Music);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfMusic_stop(IntPtr Music);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern SoundStatus sfMusic_getStatus(IntPtr Music);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern long sfMusic_getDuration(IntPtr Music);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern uint sfMusic_getChannelCount(IntPtr Music);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern uint sfMusic_getSampleRate(IntPtr Music);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfMusic_setPitch(IntPtr Music, float Pitch);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfMusic_setLoop(IntPtr Music, bool Loop);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfMusic_setVolume(IntPtr Music, float Volume);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfMusic_setPosition(IntPtr Music, Vector3f position);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfMusic_setRelativeToListener(IntPtr Music, bool Relative);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfMusic_setMinDistance(IntPtr Music, float MinDistance);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfMusic_setAttenuation(IntPtr Music, float Attenuation);
            
            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfMusic_setPlayingOffset(IntPtr Music, long TimeOffset);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern bool sfMusic_getLoop(IntPtr Music);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern float sfMusic_getPitch(IntPtr Music);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern float sfMusic_getVolume(IntPtr Music);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern Vector3f sfMusic_getPosition(IntPtr Music);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern bool sfMusic_isRelativeToListener(IntPtr Music);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern float sfMusic_getMinDistance(IntPtr Music);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern float sfMusic_getAttenuation(IntPtr Music);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern long sfMusic_getPlayingOffset(IntPtr Music);

            #endregion
        }
    }
}
