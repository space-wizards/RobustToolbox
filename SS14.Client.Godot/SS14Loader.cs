using Godot;
using System;
using System.Reflection;
using System.Collections.Generic;

// NOT SS14.Client.Godot so you can still use Godot.xxx without a using statement.
namespace SS14.Client.GodotGlue
{
    /// <summary>
    ///     AutoLoad Node that starts the rest of the SS14 Client through <see cref="ClientEntryPoint"/>.
    /// </summary>
    public class SS14Loader : Node
    {
        public bool ShuttingDown { get; private set; } = false;
        public Assembly SS14Assembly { get; private set; }
        public IReadOnlyList<ClientEntryPoint> EntryPoints => entryPoints;
        private List<ClientEntryPoint> entryPoints = new List<ClientEntryPoint>();

        private bool Started = false;

        public override void _Ready()
        {
            CallDeferred(nameof(AnnounceMain));
        }

        public void AnnounceMain()
        {
            if (Started)
            {
                return;
            }
            Started = true;
            SS14Assembly = Assembly.LoadFrom("../bin/Client/SS14.Client.dll");
            var entryType = typeof(ClientEntryPoint);
            foreach (var type in SS14Assembly.GetTypes())
            {
                if (entryType.IsAssignableFrom(type) && !type.IsAbstract)
                {
                    var instance = (ClientEntryPoint)Activator.CreateInstance(type);
                    try
                    {
                        instance.Main(GetTree());
                    }
                    catch (Exception e)
                    {
                        GD.Print($"Caught exception inside Main:\n{e}");
                        GD.Print("TO PREVENT DATA LOSS OR LOG SPAM MAKING THE FORMER A PAIN TO FIND, THIS IS A FATAL ERROR.");
                        GetTree().Quit();
                        return;
                    }
                    entryPoints.Add(instance);
                }
            }
        }

        public override void _PhysicsProcess(float delta)
        {
            if (!ShuttingDown)
            {
                foreach (var entrypoint in EntryPoints)
                {
                    try
                    {
                        entrypoint.PhysicsProcess(delta);
                    }
                    catch (Exception e)
                    {
                        GD.Print($"Caught exception inside PhysicsProcess:\n{e}");
                    }
                }
            }
        }

        public override void _Process(float delta)
        {
            if (!ShuttingDown)
            {
                foreach (var entrypoint in EntryPoints)
                {
                    entrypoint.FrameProcess(delta);
                }
            }
        }

        public override void _UnhandledInput(InputEvent inputEvent)
        {
            if (!ShuttingDown)
            {
                foreach (var entrypoint in EntryPoints)
                {
                    entrypoint.Input(inputEvent);
                }
            }
        }

        public override void _Input(InputEvent inputEvent)
        {
            if (!ShuttingDown)
            {
                foreach (var entrypoint in EntryPoints)
                {
                    entrypoint.PreInput(inputEvent);
                }
            }
        }

        public override void _Notification(int what)
        {
            if (!ShuttingDown)
            {
                switch (what)
                {
                    case MainLoop.NotificationWmQuitRequest:
                        ShuttingDown = true;
                        foreach (var entrypoint in EntryPoints)
                        {
                            entrypoint.QuitRequest();
                        }
                        break;
                }
            }
        }
    }

    /// <summary>
    ///     Automatically gets created on AutoLoad by <see cref="ClientEntryPoint"/>.
    /// </summary>
    public abstract class ClientEntryPoint
    {
        /// <summary>
        ///     Called when the entry point gets created.
        /// </summary>
        public virtual void Main(SceneTree tree)
        {
        }

        /// <summary>
        ///     Called every rendering frame by Godot.
        /// </summary>
        /// <param name="delta">Time delta since last process tick.</param>
        public virtual void FrameProcess(float delta)
        {
        }

        /// <summary>
        ///     Called every physics process by Godot.
        ///     This should be a fixed update rate.
        /// </summary>
        /// <param name="delta">
        ///     Time delta since the last process tick.
        ///     Should be constant if the CPU isn't being tortured.
        /// </param>
        public virtual void PhysicsProcess(float delta)
        {
        }

        /// <summary>
        ///     Called whenever we receive input.
        ///     The UI system can still intercept this beforehand.
        /// </summary>
        public virtual void Input(InputEvent inputEvent)
        {
        }

        /// <summary>
        ///     Called before all other input events. This is before the UI system.
        /// </summary>
        public virtual void PreInput(InputEvent inputEvent)
        {
        }

        /// <summary>
        ///     Called when the OS sends a quit request, such as the user clicking the window's close button.
        /// </summary>
        public virtual void QuitRequest()
        {
        }
    }
}
