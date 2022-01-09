using System.Collections.Concurrent;
using OpenToolkit.Audio.OpenAL;

namespace Robust.Client.Graphics.Audio
{
    internal partial class ClydeAudio
    {
        // Used to track audio sources that were disposed in the finalizer thread,
        // so we need to properly send them off in the main thread.
        private readonly ConcurrentQueue<(int sourceHandle, int filterHandle)> _sourceDisposeQueue = new();
        private readonly ConcurrentQueue<(int sourceHandle, int filterHandle)> _bufferedSourceDisposeQueue = new();
        private readonly ConcurrentQueue<int> _bufferDisposeQueue = new();

        private void _flushALDisposeQueues()
        {
            // Clear out finalized audio sources.
            while (_sourceDisposeQueue.TryDequeue(out var handles))
            {
                _openALSawmill.Debug("Cleaning out source {0} which finalized in another thread.", handles.sourceHandle);
                if (IsEfxSupported) RemoveEfx(handles);
                AL.DeleteSource(handles.sourceHandle);
                _checkAlError();
                _audioSources.Remove(handles.sourceHandle);
            }

            // Clear out finalized buffered audio sources.
            while (_bufferedSourceDisposeQueue.TryDequeue(out var handles))
            {
                _openALSawmill.Debug("Cleaning out buffered source {0} which finalized in another thread.", handles.sourceHandle);
                if (IsEfxSupported) RemoveEfx(handles);
                AL.DeleteSource(handles.sourceHandle);
                _checkAlError();
                _bufferedAudioSources.Remove(handles.sourceHandle);
            }

            // Clear out finalized audio buffers.
            while (_bufferDisposeQueue.TryDequeue(out var handle))
            {
                AL.DeleteBuffer(handle);
                _checkAlError();
            }
        }

        private void DeleteSourceOnMainThread(int sourceHandle, int filterHandle)
        {
            _sourceDisposeQueue.Enqueue((sourceHandle, filterHandle));
        }

        private void DeleteBufferedSourceOnMainThread(int bufferedSourceHandle, int filterHandle)
        {
            _bufferedSourceDisposeQueue.Enqueue((bufferedSourceHandle, filterHandle));
        }

        private void DeleteAudioBufferOnMainThread(int bufferHandle)
        {
            _bufferDisposeQueue.Enqueue(bufferHandle);
        }
    }
}
