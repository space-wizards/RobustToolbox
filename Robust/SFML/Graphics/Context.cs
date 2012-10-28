using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Runtime.ConstrainedExecution;

namespace SFML
{
    namespace Graphics
    {
        ////////////////////////////////////////////////////////////
        /// <summary>
        /// This class defines
        /// </summary>
        ////////////////////////////////////////////////////////////
        internal class Context : CriticalFinalizerObject
        {
            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Default constructor
            /// </summary>
            ////////////////////////////////////////////////////////////
            public Context()
            {
                myThis = sfContext_create();
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Finalizer
            /// </summary>
            ////////////////////////////////////////////////////////////
            ~Context()
            {
                sfContext_destroy(myThis);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Activate or deactivate the context
            /// </summary>
            /// <param name="active">True to activate, false to deactivate</param>
            ////////////////////////////////////////////////////////////
            public void SetActive(bool active)
            {
                sfContext_setActive(myThis, active);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Global helper context
            /// </summary>
            ////////////////////////////////////////////////////////////
            public static Context Global
            {
                get
                {
                    if (ourGlobalContext == null)
                        ourGlobalContext = new Context();

                    return ourGlobalContext;
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
                return "[Context]";
            }

            private static Context ourGlobalContext = null;

            private IntPtr myThis = IntPtr.Zero;

            #region Imports
            [DllImport("csfml-window-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntPtr sfContext_create();

            [DllImport("csfml-window-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfContext_destroy(IntPtr View);

            [DllImport("csfml-window-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfContext_setActive(IntPtr View, bool Active);
            #endregion
        }
    }
}
