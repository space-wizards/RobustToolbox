using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using SFML.Audio;

namespace SFML.Graphics
{
    /// <summary>
    /// An implementation of <see cref="SoundBuffer"/> that will load on-demand.
    /// </summary>
    public class LazySoundBuffer : SoundBuffer
    {
        string _filename;
        LazyContentLoadFailCounter _loadFailCounter;

        /// <summary>
        /// Initializes a new instance of the <see cref="LazySoundBuffer"/> class.
        /// </summary>
        /// <param name="filename">Path of the sound file to load</param>
        public LazySoundBuffer(string filename)
        {
            _filename = filename;
        }

        /// <summary>
        /// Gets the file name that this sound buffer uses to load.
        /// </summary>
        public string FileName
        {
            get { return _filename; }
        }

        /// <summary>
        /// Gets if this object has been disposed. For objects that will automatically reload after being disposed (such as content
        /// loaded through a ContentManager), this will always return true since, even if the object is disposed at the time the
        /// call is made to the method, it will reload automatically when needed. Such objects are often variations of existing
        /// objects, prefixed with the word "Lazy" (e.g. Image and LazyImage).
        /// </summary>
        public override bool IsDisposed
        {
            get { return false; }
        }

        /// <summary>
        /// Access to the internal pointer of the object.
        /// For internal use only
        /// </summary>
        public override IntPtr This
        {
            get
            {
                // Check for a valid file name
                if (FileName != null)
                {
                    // Check if enough time has elapsed to try loading again
                    if (_loadFailCounter == null || _loadFailCounter.HasEnoughTimeElapsed)
                    {
                        try
                        {
                            // Reload (if needed)
                            if (!EnsureLoaded(FileName))
                                OnReload();

                            // Loading was successful
                            _loadFailCounter = null;
                        }
                        catch (LoadingFailedException ex)
                        {
                            // Loading was not successful
                            if (_loadFailCounter == null)
                                _loadFailCounter = new LazyContentLoadFailCounter();

                            _loadFailCounter.HandleLoadException(this, ex);
                        }
                    }
                }

                return base.This;
            }
        }

        /// <summary>
        /// Handle the destruction of the object
        /// </summary>
        /// <param name="disposing">Is the GC disposing the object, or is it an explicit call ?</param>
        protected override void Destroy(bool disposing)
        {
            if (!disposing)
                _filename = null;

            base.Destroy(disposing);
        }

        /// <summary>
        /// Explicitely dispose the object
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2215:Dispose methods should call base class dispose")]
        public override void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// When overridden in the derived class, handles when the <see cref="LazyImage"/> is reloaded.
        /// </summary>
        protected virtual void OnReload()
        {
        }
    }
}