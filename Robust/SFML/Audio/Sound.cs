using System;
using System.Runtime.InteropServices;
using System.Security;

namespace SFML
{
    namespace Audio
    {
        ////////////////////////////////////////////////////////////
        /// <summary>
        /// Enumeration of all possible sound states
        /// </summary>
        ////////////////////////////////////////////////////////////
        public enum SoundStatus
        {
            /// <summary>Sound is not playing</summary>
            Stopped,

            /// <summary>Sound is paused</summary>
            Paused,

            /// <summary>Sound is playing</summary>
            Playing
        }

        ////////////////////////////////////////////////////////////
        /// <summary>
        /// Sound defines the properties of a sound such as position,
        /// volume, pitch, etc.
        /// </summary>
        ////////////////////////////////////////////////////////////
        public class Sound : ObjectBase
        {
            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Default constructor (invalid sound)
            /// </summary>
            ////////////////////////////////////////////////////////////
            public Sound() :
                base(sfSound_create())
            {
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct the sound from a source buffer
            /// </summary>
            /// <param name="buffer">Sound buffer to play</param>
            ////////////////////////////////////////////////////////////
            public Sound(SoundBuffer buffer) :
                base(sfSound_create())
            {
                SoundBuffer = buffer;
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct the sound from another source
            /// </summary>
            /// <param name="copy">Sound to copy</param>
            ////////////////////////////////////////////////////////////
            public Sound(Sound copy) :
                base(sfSound_copy(copy.CPointer))
            {
                SoundBuffer = copy.SoundBuffer;
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Play the sound
            /// </summary>
            ////////////////////////////////////////////////////////////
            public void Play()
            {
                sfSound_play(CPointer);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Pause the sound
            /// </summary>
            ////////////////////////////////////////////////////////////
            public void Pause()
            {
                sfSound_pause(CPointer);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Stop the sound
            /// </summary>
            ////////////////////////////////////////////////////////////
            public void Stop()
            {
                sfSound_stop(CPointer);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Buffer containing the sound data to play through the sound
            /// </summary>
            ////////////////////////////////////////////////////////////
            public SoundBuffer SoundBuffer
            {
                get {return myBuffer;}
                set {myBuffer = value; sfSound_setBuffer(CPointer, value != null ? value.CPointer : IntPtr.Zero);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Current status of the sound (see SoundStatus enum)
            /// </summary>
            ////////////////////////////////////////////////////////////
            public SoundStatus Status
            {
                get {return sfSound_getStatus(CPointer);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Loop state of the sound. Default value is false
            /// </summary>
            ////////////////////////////////////////////////////////////
            public bool Loop
            {
                get {return sfSound_getLoop(CPointer);}
                set {sfSound_setLoop(CPointer, value);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Pitch of the sound. Default value is 1
            /// </summary>
            ////////////////////////////////////////////////////////////
            public float Pitch
            {
                get {return sfSound_getPitch(CPointer);}
                set {sfSound_setPitch(CPointer, value);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Volume of the sound, in range [0, 100]. Default value is 100
            /// </summary>
            ////////////////////////////////////////////////////////////
            public float Volume
            {
                get {return sfSound_getVolume(CPointer);}
                set {sfSound_setVolume(CPointer, value);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Current playing position of the sound, in milliseconds
            /// </summary>
            ////////////////////////////////////////////////////////////
            public TimeSpan PlayingOffset
            {
                get
                {
                    long microseconds = sfSound_getPlayingOffset(CPointer);
                    return TimeSpan.FromTicks(microseconds * TimeSpan.TicksPerMillisecond / 1000);
                }
                set
                {
                    long microseconds = value.Ticks / (TimeSpan.TicksPerMillisecond / 1000);
                    sfSound_setPlayingOffset(CPointer, microseconds);
                }
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// 3D position of the sound. Default value is (0, 0, 0)
            /// </summary>
            ////////////////////////////////////////////////////////////
            public Vector3f Position
            {
                get {return sfSound_getPosition(CPointer);}
                set {sfSound_setPosition(CPointer, value);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Is the sound's position relative to the listener's position,
            /// or is it absolute?
            /// Default value is false (absolute)
            /// </summary>
            ////////////////////////////////////////////////////////////
            public bool RelativeToListener
            {
                get {return sfSound_isRelativeToListener(CPointer);}
                set {sfSound_setRelativeToListener(CPointer, value);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Minimum distance of the sound. Closer than this distance,
            /// the listener will hear the sound at its maximum volume.
            /// The default value is 1
            /// </summary>
            ////////////////////////////////////////////////////////////
            public float MinDistance
            {
                get {return sfSound_getMinDistance(CPointer);}
                set {sfSound_setMinDistance(CPointer, value);}
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
                get {return sfSound_getAttenuation(CPointer);}
                set {sfSound_setAttenuation(CPointer, value);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Provide a string describing the object
            /// </summary>
            /// <returns>String description of the object</returns>
            ////////////////////////////////////////////////////////////
            public override string ToString()
            {
                return "[Sound]" +
                       " Status(" + Status + ")" +
                       " Loop(" + Loop + ")" +
                       " Pitch(" + Pitch + ")" +
                       " Volume(" + Volume + ")" +
                       " Position(" + Position + ")" +
                       " RelativeToListener(" + RelativeToListener + ")" +
                       " MinDistance(" + MinDistance + ")" +
                       " Attenuation(" + Attenuation + ")" +
                       " PlayingOffset(" + PlayingOffset + ")" +
                       " SoundBuffer(" + SoundBuffer + ")";
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Handle the destruction of the object
            /// </summary>
            /// <param name="disposing">Is the GC disposing the object, or is it an explicit call ?</param>
            ////////////////////////////////////////////////////////////
            protected override void Destroy(bool disposing)
            {
                sfSound_destroy(CPointer);
            }

            private SoundBuffer myBuffer;

            #region Imports
            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntPtr sfSound_create();

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntPtr sfSound_copy(IntPtr Sound);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSound_destroy(IntPtr Sound);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSound_play(IntPtr Sound);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSound_pause(IntPtr Sound);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSound_stop(IntPtr Sound);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSound_setBuffer(IntPtr Sound, IntPtr Buffer);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntPtr sfSound_getBuffer(IntPtr Sound);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSound_setLoop(IntPtr Sound, bool Loop);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern bool sfSound_getLoop(IntPtr Sound);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern SoundStatus sfSound_getStatus(IntPtr Sound);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSound_setPitch(IntPtr Sound, float Pitch);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSound_setVolume(IntPtr Sound, float Volume);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSound_setPosition(IntPtr Sound, Vector3f position);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSound_setRelativeToListener(IntPtr Sound, bool Relative);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSound_setMinDistance(IntPtr Sound, float MinDistance);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSound_setAttenuation(IntPtr Sound, float Attenuation);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSound_setPlayingOffset(IntPtr Sound, long TimeOffset);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern float sfSound_getPitch(IntPtr Sound);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern float sfSound_getVolume(IntPtr Sound);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern Vector3f sfSound_getPosition(IntPtr Sound);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern bool sfSound_isRelativeToListener(IntPtr Sound);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern float sfSound_getMinDistance(IntPtr Sound);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern float sfSound_getAttenuation(IntPtr Sound);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern long sfSound_getPlayingOffset(IntPtr Sound);

            #endregion
        }
    }
}
