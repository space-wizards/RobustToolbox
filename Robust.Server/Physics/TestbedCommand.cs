// MIT License

// Copyright (c) 2019 Erin Catto

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.


using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Server.Physics
{
    /// <summary>
    ///     Copies of Box2D's physics testbed for debugging.
    /// </summary>
    public class TestbedCommand : IClientCommand
    {
        public string Command => "testbed";
        public string Description => "Loads a physics testbed and teleports your player there";
        public string Help => $"{Command} <test>";
        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            if (args.Length != 1)
            {
                shell.SendText(player, "Only accept 1 arg for testbed!");
                return;
            }

            switch (args[0])
            {
                case "boxstack":
                    SetupPlayer();
                    CreateBoxStack();
                    break;
                default:
                    shell.SendText(player, $"testbed {args[0]} not found!");
                    return;
            }
        }

        // TODO: Try actually adding in SynchronizeFixtures and have it whenever the body is moved it's dirty???

        private void SetupPlayer()
        {
            // aghost player
            // Pause map?
            // Send player to map.
        }

        private void CreateBoxStack()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();

            // TODO: Need a blank entity we can spawn for testbed.
            // var ground = entityManager.Sp
        }
    }
}
