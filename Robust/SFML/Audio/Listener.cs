using System;
using System.Runtime.InteropServices;
using System.Security;

namespace SFML
{
    namespace Audio
    {
        ////////////////////////////////////////////////////////////
        /// <summary>
        /// Listener is a global interface for defining the audio
        /// listener properties ; the audio listener is the point in
        /// the scene from where all the sounds are heard
        /// </summary>
        ////////////////////////////////////////////////////////////
        public class Listener
        {
            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Global volume of all sounds, in range [0 .. 100] (default is 100)
            /// </summary>
            ////////////////////////////////////////////////////////////
            public static float GlobalVolume
            {
                get {return sfListener_getGlobalVolume();}
                set {sfListener_setGlobalVolume(value);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// 3D position of the listener (default is (0, 0, 0))
            /// </summary>
            ////////////////////////////////////////////////////////////
            public static Vector3f Position
            {
                get {return sfListener_getPosition();}
                set {sfListener_setPosition(value);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// 3D direction of the listener (default is (0, 0, -1))
            /// </summary>
            ////////////////////////////////////////////////////////////
            public static Vector3f Direction
            {
                get {return sfListener_getDirection();}
                set {sfListener_setDirection(value);}
            }

            #region Imports
            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfListener_setGlobalVolume(float Volume);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern float sfListener_getGlobalVolume();

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfListener_setPosition(Vector3f position);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern Vector3f sfListener_getPosition();

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfListener_setDirection(Vector3f direction);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern Vector3f sfListener_getDirection();
            #endregion
        }
    }
}
