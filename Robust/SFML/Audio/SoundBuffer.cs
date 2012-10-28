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
        /// SoundBuffer is the low-level class for loading and manipulating
        /// sound buffers. A sound buffer holds audio data (samples)
        /// which can then be played by a Sound or saved to a file.
        /// </summary>
        ////////////////////////////////////////////////////////////
        public class SoundBuffer : ObjectBase
        {
            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct the sound buffer from a file
            /// </summary>
            /// <param name="filename">Path of the sound file to load</param>
            /// <exception cref="LoadingFailedException" />
            ////////////////////////////////////////////////////////////
            public SoundBuffer(string filename) :
                base(sfSoundBuffer_createFromFile(filename))
            {
                if (CPointer == IntPtr.Zero)
                    throw new LoadingFailedException("sound buffer", filename);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Load the sound buffer from a custom stream
            /// </summary>
            /// <param name="stream">Source stream to read from</param>
            /// <exception cref="LoadingFailedException" />
            ////////////////////////////////////////////////////////////
            public SoundBuffer(Stream stream) :
                base(IntPtr.Zero)
            {
                using (StreamAdaptor adaptor = new StreamAdaptor(stream))
                {
                    SetThis(sfSoundBuffer_createFromStream(adaptor.InputStreamPtr));
                }

                if (CPointer == IntPtr.Zero)
                    throw new LoadingFailedException("sound buffer");
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct the sound buffer from an array of samples
            /// </summary>
            /// <param name="samples">Array of samples</param>
            /// <param name="channelCount">Channel count</param>
            /// <param name="sampleRate">Sample rate</param>
            /// <exception cref="LoadingFailedException" />
            ////////////////////////////////////////////////////////////
            public SoundBuffer(short[] samples, uint channelCount, uint sampleRate) :
                base(IntPtr.Zero)
            {
                unsafe
                {
                    fixed (short* SamplesPtr = samples)
                    {
                        SetThis(sfSoundBuffer_createFromSamples(SamplesPtr, (uint)samples.Length, channelCount, sampleRate));
                    }
                }

                if (CPointer == IntPtr.Zero)
                    throw new LoadingFailedException("sound buffer");
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Construct the sound buffer from another sound buffer
            /// </summary>
            /// <param name="copy">Sound buffer to copy</param>
            ////////////////////////////////////////////////////////////
            public SoundBuffer(SoundBuffer copy) :
                base(sfSoundBuffer_copy(copy.CPointer))
            {
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Save the sound buffer to an audio file
            /// </summary>
            /// <param name="filename">Path of the sound file to write</param>
            /// <returns>True if saving has been successful</returns>
            ////////////////////////////////////////////////////////////
            public bool SaveToFile(string filename)
            {
                return sfSoundBuffer_saveToFile(CPointer, filename);
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Samples rate, in samples per second
            /// </summary>
            ////////////////////////////////////////////////////////////
            public uint SampleRate
            {
                get {return sfSoundBuffer_getSampleRate(CPointer);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Number of channels (1 = mono, 2 = stereo)
            /// </summary>
            ////////////////////////////////////////////////////////////
            public uint ChannelCount
            {
                get {return sfSoundBuffer_getChannelCount(CPointer);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Total duration of the buffer, in milliseconds
            /// </summary>
            ////////////////////////////////////////////////////////////
            public uint Duration
            {
                get {return sfSoundBuffer_getDuration(CPointer);}
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Array of samples contained in the buffer
            /// </summary>
            ////////////////////////////////////////////////////////////
            public short[] Samples
            {
                get
                {
                    short[] SamplesArray = new short[sfSoundBuffer_getSampleCount(CPointer)];
                    Marshal.Copy(sfSoundBuffer_getSamples(CPointer), SamplesArray, 0, SamplesArray.Length);
                    return SamplesArray;
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
                return "[SoundBuffer]" +
                       " SampleRate(" + SampleRate + ")" +
                       " ChannelCount(" + ChannelCount + ")" +
                       " Duration(" + Duration + ")";
            }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Handle the destruction of the object
            /// </summary>
            /// <param name="disposing">Is the GC disposing the object, or is it an explicit call ?</param>
            ////////////////////////////////////////////////////////////
            protected override void Destroy(bool disposing)
            {
                sfSoundBuffer_destroy(CPointer);
            }

            #region Imports
            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntPtr sfSoundBuffer_createFromFile(string Filename);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            unsafe static extern IntPtr sfSoundBuffer_createFromStream(IntPtr stream);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            unsafe static extern IntPtr sfSoundBuffer_createFromSamples(short* Samples, uint SampleCount, uint ChannelsCount, uint SampleRate);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntPtr sfSoundBuffer_copy(IntPtr SoundBuffer);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern void sfSoundBuffer_destroy(IntPtr SoundBuffer);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern bool sfSoundBuffer_saveToFile(IntPtr SoundBuffer, string Filename);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern IntPtr sfSoundBuffer_getSamples(IntPtr SoundBuffer);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern uint sfSoundBuffer_getSampleCount(IntPtr SoundBuffer);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern uint sfSoundBuffer_getSampleRate(IntPtr SoundBuffer);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern uint sfSoundBuffer_getChannelCount(IntPtr SoundBuffer);

            [DllImport("csfml-audio-2", CallingConvention = CallingConvention.Cdecl), SuppressUnmanagedCodeSecurity]
            static extern uint sfSoundBuffer_getDuration(IntPtr SoundBuffer);
            #endregion
        }
    }
}
