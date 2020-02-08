using System;
using Robust.Shared.Maths;

namespace Robust.Client.Interfaces.Graphics
{
    public interface IClydeAudioSource : IDisposable
    {
        void StartPlaying();
        void StopPlaying();

        bool IsPlaying { get; }

        bool IsLooping { get; set; }

        void SetPosition(Vector2 position);
        void SetPitch(float pitch);
        void SetGlobal();
        void SetVolume(float decibels);
        void SetPlaybackPosition(float seconds);
    }
}