using System.Collections.Generic;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    internal sealed class PhysicsMap
    {
        // AKA world.

        private ContactManager _contactManager = default!;

        /// <summary>
        ///     All bodies present on this map.
        /// </summary>
        public HashSet<PhysicsComponent> Bodies = new();

        /// <summary>
        ///     All awake bodies on this map.
        /// </summary>
        public HashSet<PhysicsComponent> AwakeBodies = new();

        // Queued map changes
        private HashSet<PhysicsComponent> _queuedBodyAdd = new();
        private HashSet<PhysicsComponent> _queuedBodyRemove = new();

        public void Initialize()
        {
            IoCManager.InjectDependencies(this);
            _contactManager = new ContactManager();
            _contactManager.Initialize();
        }

        #region AddRemove
        public void AddBody(PhysicsComponent body)
        {
            // DebugTools.Assert(!_queuedBodyAdd.Contains(body));
            _queuedBodyAdd.Add(body);
        }

        public void RemoveBody(PhysicsComponent body)
        {
            // DebugTools.Assert(!_queuedBodyRemove.Contains(body));
            _queuedBodyRemove.Add(body);
        }

        // TODO: Someday joints too.

        #endregion

        #region Queue
        private void ProcessChanges()
        {
            ProcessAddQueue();
            ProcessRemoveQueue();
        }

        private void ProcessAddQueue()
        {
            foreach (var body in _queuedBodyAdd)
            {
                Bodies.Add(body);

                if (body.Awake)
                {
                    AwakeBodies.Add(body);
                }
            }

            _queuedBodyAdd.Clear();
        }

        private void ProcessRemoveQueue()
        {
            foreach (var body in _queuedBodyRemove)
            {
                Bodies.Remove(body);

                if (body.Awake)
                {
                    AwakeBodies.Remove(body);
                }
            }

            _queuedBodyRemove.Clear();
        }
        #endregion

        /// <summary>
        ///     Where the magic happens
        /// </summary>
        /// <param name="frameTime"></param>
        public void Step(float frameTime)
        {
            ProcessChanges();

            // Find collisions up front
            _contactManager.FindNewContacts(this);
        }
    }
}
