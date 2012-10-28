using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Security;
using System.IO;

namespace SFML
{
    namespace Window
    {
        ////////////////////////////////////////////////////////////
        /// <summary>
        /// Structure that contains InputStream callbacks
        /// (directly maps to a CSFML sfInputStream)
        /// </summary>
        ////////////////////////////////////////////////////////////
        [StructLayout(LayoutKind.Sequential)]
        public struct InputStream
        {
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate long ReadCallbackType(IntPtr data, long size, IntPtr userData);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate long SeekCallbackType(long position, IntPtr userData);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate long TellCallbackType(IntPtr userData);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate long GetSizeCallbackType(IntPtr userData);

            public ReadCallbackType Read;
            public SeekCallbackType Seek;
            public TellCallbackType Tell;
            public GetSizeCallbackType GetSize;
        }

        ////////////////////////////////////////////////////////////
        /// <summary>
        /// Adapts a System.IO.Stream to be usable as a SFML InputStream
        /// </summary>
        ////////////////////////////////////////////////////////////
        public class StreamAdaptor : IDisposable
        {
            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct from a System.IO.Stream
            /// </summary>
            /// <param name="stream">Stream to adapt</param>
            ////////////////////////////////////////////////////////////
            public StreamAdaptor(Stream stream)
            {
                myStream = stream;

                myInputStream = new InputStream();
                myInputStream.Read = new InputStream.ReadCallbackType(Read);
                myInputStream.Seek = new InputStream.SeekCallbackType(Seek);
                myInputStream.Tell = new InputStream.TellCallbackType(Tell);
                myInputStream.GetSize = new InputStream.GetSizeCallbackType(GetSize);

                myInputStreamPtr = Marshal.AllocHGlobal(Marshal.SizeOf(myInputStream));
                Marshal.StructureToPtr(myInputStream, myInputStreamPtr, false);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Dispose the object
            /// </summary>
            ////////////////////////////////////////////////////////////
            ~StreamAdaptor()
            {
                Dispose(false);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// The pointer to the CSFML InputStream structure
            /// </summary>
            ////////////////////////////////////////////////////////////
            public IntPtr InputStreamPtr
            {
                get {return myInputStreamPtr;}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Explicitely dispose the object
            /// </summary>
            ////////////////////////////////////////////////////////////
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Destroy the object
            /// </summary>
            /// <param name="disposing">Is the GC disposing the object, or is it an explicit call ?</param>
            ////////////////////////////////////////////////////////////
            private void Dispose(bool disposing)
            {
                Marshal.FreeHGlobal(myInputStreamPtr);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Called to read from the stream
            /// </summary>
            /// <param name="data">Where to copy the read bytes</param>
            /// <param name="size">Size to read, in bytes</param>
            /// <param name="userData">User data -- unused</param>
            /// <returns>Number of bytes read</returns>
            ////////////////////////////////////////////////////////////
            private long Read(IntPtr data, long size, IntPtr userData)
            {
                byte[] buffer = new byte[size];
                int count = myStream.Read(buffer, 0, (int)size);
                Marshal.Copy(buffer, 0, data, count);
                return count;
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Called to set the read position in the stream
            /// </summary>
            /// <param name="position">New read position</param>
            /// <param name="userData">User data -- unused</param>
            /// <returns>Actual position</returns>
            ////////////////////////////////////////////////////////////
            private long Seek(long position, IntPtr userData)
            {
                return myStream.Seek(position, System.IO.SeekOrigin.Begin);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Get the current read position in the stream
            /// </summary>
            /// <param name="userData">User data -- unused</param>
            /// <returns>Current position in the stream</returns>
            ////////////////////////////////////////////////////////////
            private long Tell(IntPtr userData)
            {
                return myStream.Position;
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Called to get the total size of the stream
            /// </summary>
            /// <param name="userData">User data -- unused</param>
            /// <returns>Number of bytes in the stream</returns>
            ////////////////////////////////////////////////////////////
            private long GetSize(IntPtr userData)
            {
                return myStream.Length;
            }

            private Stream myStream;
            private InputStream myInputStream;
            private IntPtr myInputStreamPtr;
        }
    }
}
