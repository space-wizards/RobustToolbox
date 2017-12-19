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
        public Assembly SS14Assembly { get; private set; }
        public IReadOnlyList<ClientEntryPoint> EntryPoints => entryPoints;
        private List<ClientEntryPoint> entryPoints = new List<ClientEntryPoint>();

        public override void _Ready()
        {
            SS14Assembly = Assembly.LoadFrom("../bin/Client/SS14.Client.dll");
            var entryType = typeof(ClientEntryPoint);
            foreach (var type in SS14Assembly.GetTypes())
            {
                if (entryType.IsAssignableFrom(type) && !type.IsAbstract)
                {
                    var instance = (ClientEntryPoint)Activator.CreateInstance(type);
                    try
                    {
                        instance.Main();
                    }
                    catch (Exception e)
                    {
                        GD.Print($"Caught exception inside ClientEntryPoint Main:\n{e}");
                    }
                    entryPoints.Add(instance);
                }
            }
        }

        public override void _PhysicsProcess(float delta)
        {
            foreach (var entrypoint in EntryPoints)
            {
                entrypoint.PhysicsProcess(delta);
            }
        }

        public override void _Process(float delta)
        {
            foreach (var entrypoint in EntryPoints)
            {
                entrypoint.FrameProcess(delta);
            }
        }

        public override void _Input(InputEvent inputEvent)
        {
            foreach (var entrypoint in EntryPoints)
            {
                entrypoint.Input(inputEvent);
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
        public virtual void Main()
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
        ///     Called whenever Godot receives input.
        /// </summary>
        public virtual void Input(InputEvent inputEvent)
        {
        }
    }
}
