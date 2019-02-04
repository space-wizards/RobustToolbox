using System;
using System.Collections.Generic;
using Godot;
using SS14.Client.GodotGlue;
using SS14.Client.Input;
using SS14.Client.Interfaces;
using SS14.Client.Utility;
using SS14.Shared.ContentPack;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Timing;

namespace SS14.Client
{
    internal partial class GameController : ClientEntryPoint
    {
        private GameTimingGodot _gameTimingGodotGodot;

        public override void Main(Godot.SceneTree tree)
        {
#if !X64
            throw new InvalidOperationException("The client cannot start outside x64.");
#endif
            Mode = DisplayMode.Godot;
            tree.SetAutoAcceptQuit(false);
            IoCManager.Register<ISceneTreeHolder, SceneTreeHolder>();
            IoCManager.BuildGraph();
            IoCManager.Resolve<ISceneTreeHolder>().Initialize(tree);

#if DEBUG
            // Ensure Godot's side of the resources are up to date.
            GodotResourceCopy.DoDirCopy("../Resources", "Engine");
#endif

            Startup();

            _gameTimingGodotGodot = IoCManager.Resolve<GameTimingGodot>();
        }

        public override void QuitRequest()
        {
            Shutdown("OS quit request");
        }

        public override void PhysicsProcess(float delta)
        {
            // Can't be too certain.
            _gameTimingGodotGodot.InSimulation = true;
            _gameTimingGodotGodot._tickRemainderTimer.Restart();
            try
            {
                if (!_gameTimingGodotGodot.Paused)
                {
                    _gameTimingGodotGodot.CurTick++;
                    Update(delta);
                }
            }
            finally
            {
                _gameTimingGodotGodot.InSimulation = false;
            }
        }

        public override void FrameProcess(float delta)
        {
            _gameTimingGodotGodot.InSimulation = false; // Better safe than sorry.
            _gameTimingGodotGodot.RealFrameTime = TimeSpan.FromSeconds(delta);
            _gameTimingGodotGodot.TickRemainder = _gameTimingGodotGodot._tickRemainderTimer.Elapsed;

            _frameProcessMain(delta);
        }

        private void _frameProcessMain(float delta)
        {
            var eventArgs = new RenderFrameEventArgs(delta);
            AssemblyLoader.BroadcastUpdate(AssemblyLoader.UpdateLevel.FramePreEngine, eventArgs.Elapsed);
            _lightManager.FrameUpdate(eventArgs);
            _stateManager.FrameUpdate(eventArgs);
            _overlayManager.FrameUpdate(eventArgs);
            AssemblyLoader.BroadcastUpdate(AssemblyLoader.UpdateLevel.FramePostEngine, eventArgs.Elapsed);
        }

        public override void HandleException(Exception exception)
        {
            try
            {
                if (_logManager != null)
                {
                    _logManager.GetSawmill("root").Error($"Unhandled exception:\n{exception}");
                }
                else
                {
                    Godot.GD.Print($"Unhandled exception:\n{exception}");
                }
            }
            catch (Exception e)
            {
                Godot.GD.Print($"Welp. The unhandled exception handler threw an exception.\n{e}\nException that was being handled:\n{exception}");
            }
        }


        // Override that converts and distributes the input events
        //   to the more sane methods above.
        public override void Input(Godot.InputEvent inputEvent)
        {
            switch (inputEvent)
            {
                case Godot.InputEventKey keyEvent:
                    var keyEventArgs = (KeyEventArgs)keyEvent;
                    if (keyEvent.Echo)
                    {
                        return;
                    }
                    else if (keyEvent.Pressed)
                    {
                        KeyDown(keyEventArgs);
                    }
                    else
                    {
                        KeyUp(keyEventArgs);
                    }
                    break;

                case Godot.InputEventMouseButton mouseButtonEvent:
                    if (mouseButtonEvent.ButtonIndex >= (int)Godot.ButtonList.WheelUp && mouseButtonEvent.ButtonIndex <= (int)Godot.ButtonList.WheelRight)
                    {
                        // Mouse wheel event.
                        var mouseWheelEventArgs = (MouseWheelEventArgs)mouseButtonEvent;
                        MouseWheel(mouseWheelEventArgs);
                    }
                    else
                    {
                        // Mouse button event.
                        var mouseButtonEventArgs = (MouseButtonEventArgs)mouseButtonEvent;
                        if (mouseButtonEvent.Pressed)
                        {
                            KeyDown((KeyEventArgs) mouseButtonEvent);
                            MouseDown(mouseButtonEventArgs);
                        }
                        else
                        {
                            KeyUp((KeyEventArgs)mouseButtonEvent);
                            MouseUp(mouseButtonEventArgs);
                        }
                    }
                    break;

                case Godot.InputEventMouseMotion mouseMotionEvent:
                    var mouseMoveEventArgs = (MouseMoveEventArgs)mouseMotionEvent;
                    MouseMove(mouseMoveEventArgs);
                    break;
            }
        }

        public override void PreInput(Godot.InputEvent inputEvent)
        {
            if (inputEvent is Godot.InputEventKey keyEvent)
            {
                var keyEventArgs = (KeyEventArgs)keyEvent;
                if (keyEvent.Echo)
                {
                    return;
                }
                else if (keyEvent.Pressed)
                {
                    // TODO: these hacks are in right now for toggling the debug console.
                    // Somehow find a way to make the console use the key binds system?
                    _userInterfaceManager.PreKeyDown(keyEventArgs);
                }
                else
                {
                    _userInterfaceManager.PreKeyUp(keyEventArgs);
                }
            }
        }

        // TODO: This class is basically just a bunch of stubs.
        private class GameTimingGodot : IGameTiming
        {
            private static readonly IStopwatch _realTimer = new Stopwatch();
            public readonly IStopwatch _tickRemainderTimer = new Stopwatch();

            public GameTimingGodot()
            {
                _realTimer.Start();
                // Not sure if Restart() starts it implicitly so...
                _tickRemainderTimer.Start();
            }

            public bool InSimulation { get; set; }
            public bool Paused { get; set; }

            public TimeSpan CurTime => CalcCurTime();

            public TimeSpan RealTime => _realTimer.Elapsed;

            public TimeSpan FrameTime => CalcFrameTime();

            public TimeSpan RealFrameTime { get; set; }

            public TimeSpan RealFrameTimeAvg => throw new NotImplementedException();

            public TimeSpan RealFrameTimeStdDev => throw new NotImplementedException();

            public double FramesPerSecondAvg => Godot.Performance.GetMonitor(Performance.Monitor.TimeFps);

            public uint CurTick { get; set; }
            public int TickRate
            {
                get => Godot.Engine.IterationsPerSecond;
                set => Godot.Engine.IterationsPerSecond = value;
            }

            public TimeSpan TickPeriod => TimeSpan.FromTicks((long)(1.0 / TickRate * TimeSpan.TicksPerSecond));

            public TimeSpan TickRemainder { get; set; }

            public void ResetRealTime()
            {
                throw new NotImplementedException();
            }

            public void StartFrame()
            {
                throw new NotImplementedException();
            }

            private TimeSpan CalcCurTime()
            {
                // calculate simulation CurTime
                var time = TimeSpan.FromTicks(TickPeriod.Ticks * CurTick);

                if (!InSimulation) // rendering can draw frames between ticks
                    return time + TickRemainder;
                return time;
            }

            private TimeSpan CalcFrameTime()
            {
                // calculate simulation FrameTime
                if (InSimulation)
                {
                    return TimeSpan.FromTicks(TickPeriod.Ticks);
                }
                else
                {
                    return Paused ? TimeSpan.Zero : RealFrameTime;
                }
            }
        }
    }
}
