using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using OpenTK.Audio.OpenAL;
using OpenTK.Audio.OpenAL.Extensions.Creative.EFX;
using OpenTK.Mathematics;
using Robust.Client.Audio;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Audio;
using Robust.Shared.Log;
using Robust.Shared.Timing;
using Vector2 = Robust.Shared.Maths.Vector2;

namespace Robust.Client.Graphics.Audio
{
    internal partial class ClydeAudio
    {
        [Robust.Shared.IoC.Dependency] private readonly IConfigurationManager _cfg = default!;
        [Robust.Shared.IoC.Dependency] private readonly IEyeManager _eyeManager = default!;
        [Robust.Shared.IoC.Dependency] private readonly ILogManager _logMan = default!;

        private Thread? _gameThread;

        public bool InitializePostWindowing()
        {
            _gameThread = Thread.CurrentThread;
            return _initializeAudio();
        }

        public void FrameProcess(FrameEventArgs eventArgs)
        {
            _updateAudio();
        }

        public void Shutdown()
        {
            _shutdownAudio();
        }

        private bool IsMainThread()
        {
            return Thread.CurrentThread == _gameThread;
        }
    }
}
