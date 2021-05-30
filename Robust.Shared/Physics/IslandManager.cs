using System.Collections.Generic;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    internal interface IIslandManager
    {
        void Initialize();
        void InitializePools();
        PhysicsIsland AllocateIsland(int numBodies, int numContacts, int numJoints);

        IReadOnlyList<PhysicsIsland> GetActive { get; }
    }

    /// <summary>
    /// Contains physics islands for use by PhysicsMaps.
    /// Key difference from bullet3 is we won't merge small islands together due to the way box2d sleeping works.
    /// </summary>
    public class IslandManager : IIslandManager
    {
        private IslandBodyCapacitySort _capacitySort = new();
        private IslandBodyCountSort _countSort = new();

        /// <summary>
        /// The island all non-contact non-joint bodies are added to to batch together. This needs its own custom sleeping
        /// given we cant wait for every body to be ready to sleep.
        /// </summary>
        private PhysicsIsland _loneIsland = new();// {LoneIsland = true, ID = 0};

        /// <summary>
        /// Contains islands currently in use.
        /// </summary>
        private List<PhysicsIsland> _activeIslands = new();

        private List<PhysicsIsland> _freeIslands = new();

        /// <summary>
        /// Contains every single PhysicsIsland.
        /// </summary>
        private List<PhysicsIsland> _allocatedIslands = new();

        public IReadOnlyList<PhysicsIsland> GetActive
        {
            get
            {
                if (_loneIsland.BodyCount > 0)
                {
                    _activeIslands.Add(_loneIsland);
                }

                // Look this is kinda stinky but it's only called at the appropriate place for now
                _activeIslands.Sort(_countSort);
                return _activeIslands;
            }
        }

        public void Initialize()
        {
            _loneIsland.Initialize();
        }

        public void InitializePools()
        {
            _loneIsland.Clear();
            _activeIslands.Clear();
            _freeIslands.Clear();

            // Check whether allocated islands are sorted
            // Bullet3 uses bodycapacity but constraints are way more expensive
            var lastCapacity = 0;
            var isSorted = true;

            foreach (var island in _allocatedIslands)
            {
                var capacity = island.BodyCapacity;
                if (capacity > lastCapacity)
                {
                    isSorted = false;
                    break;
                }

                lastCapacity = capacity;
            }

            if (!isSorted)
            {
                _allocatedIslands.Sort(_capacitySort);
            }

            // TODO: Look at removing islands occasionally just to avoid memory bloat over time.
            // Free up islands
            foreach (var island in _allocatedIslands)
            {
                // No need to reset ID here right...
                island.Clear();
                _freeIslands.Add(island);
            }
        }

        public PhysicsIsland AllocateIsland(int bodyCount, int contactCount, int jointCount)
        {
            // TODO: Assert the caller
            // If only 1 body then that means no contacts or joints. This also means that we can add it to the loneisland
            if (bodyCount == 1)
            {
                // Island manages re-sizes internally so don't need to worry about this being hammered.
                _loneIsland.Resize(_loneIsland.BodyCount + 1, 0, 0);
                return _loneIsland;
            }

            PhysicsIsland? island = null;

            // Because bullet3 combines islands it's more relevant for them to allocate by bodies but at least for now
            // we don't combine.
            if (_freeIslands.Count > 0)
            {
                // Try to use an existing island; with the smallest size that can hold us if possible.
                var iFound = _freeIslands.Count;

                for (var i = _freeIslands.Count - 1; i >= 0; i--)
                {
                    if (_freeIslands[i].BodyCapacity >= bodyCount)
                    {
                        iFound = i;
                        island = _freeIslands[i];
                        DebugTools.Assert(island.BodyCount == 0 && island.ContactCount == 0 && island.JointCount == 0);
                        break;
                    }
                }

                if (island != null)
                {
                    var iDest = iFound;
                    var iSrc = iDest + 1;
                    while (iSrc < _freeIslands.Count)
                    {
                        _freeIslands[iDest++] = _freeIslands[iSrc++];
                    }

                    _freeIslands.RemoveAt(_freeIslands.Count - 1);
                }
            }

            if (island == null)
            {
                island = new PhysicsIsland();
                island.Initialize();
                _allocatedIslands.Add(island);
            }

            island.Resize(bodyCount, contactCount, jointCount);
            // 0 ID taken up by LoneIsland
            island.ID = _activeIslands.Count + 1;
            _activeIslands.Add(island);
            return island;
        }
    }

    internal sealed class IslandBodyCapacitySort : Comparer<PhysicsIsland>
    {
        public override int Compare(PhysicsIsland? x, PhysicsIsland? y)
        {
            if (x == null || y == null) return 0;
            return x.BodyCapacity > y.BodyCapacity ? 1 : 0;
        }
    }

    internal sealed class IslandBodyCountSort : Comparer<PhysicsIsland>
    {
        public override int Compare(PhysicsIsland? x, PhysicsIsland? y)
        {
            if (x == null || y == null) return 0;
            return x.BodyCount > y.BodyCount ? 1 : 0;
        }
    }
}
