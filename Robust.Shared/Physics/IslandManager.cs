/*
Bullet Continuous Collision Detection and Physics Library
Copyright (c) 2003-2006 Erwin Coumans  http://continuousphysics.com/Bullet/
This software is provided 'as-is', without any express or implied warranty.
In no event will the authors be held liable for any damages arising from the use of this software.
Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it freely,
subject to the following restrictions:
1. The origin of this software must not be misrepresented; you must not claim that you wrote the original software. If you use this software in a product, an acknowledgment in the product documentation would be appreciated but is not required.
2. Altered source versions must be plainly marked as such, and must not be misrepresented as being the original software.
3. This notice may not be removed or altered from any source distribution.
*/

using System;
using System.Collections.Generic;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
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
    internal sealed class IslandManager : IIslandManager
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        private static readonly IslandBodyCapacitySort CapacitySort = new();
        private static readonly IslandBodyCountSort CountSort = new();

        /// <summary>
        /// The island all non-contact non-joint bodies are added to to batch together. This needs its own custom sleeping
        /// given we cant wait for every body to be ready to sleep.
        /// </summary>
        private PhysicsIsland _loneIsland = new() {LoneIsland = true, ID = 0};

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
                _activeIslands.Sort(CountSort);
                return _activeIslands;
            }
        }

        private int _tickRate;
        private float _maxLinearVelocityRaw;
        private float _maxAngularVelocityRaw;
        private IslandCfg _islandCfg;

        public void Initialize()
        {
            InitConfig();

            _loneIsland.Initialize();
            // Set an initial size so we don't spam a bunch of array resizes at the start
            _loneIsland.Resize(64, 32, 8);
        }

        private void InitConfig()
        {
            // Config stuff.
            CfgVar(CVars.AngularSleepTolerance, value => _islandCfg.AngTolSqr = value * value);
            CfgVar(CVars.LinearSleepTolerance, value => _islandCfg.LinTolSqr = value * value);
            CfgVar(CVars.WarmStarting, value => _islandCfg.WarmStarting = value);
            CfgVar(CVars.VelocityIterations, value => _islandCfg.VelocityIterations = value);
            CfgVar(CVars.PositionIterations, value => _islandCfg.PositionIterations = value);
            CfgVar(CVars.SleepAllowed, value => _islandCfg.SleepAllowed = value);
            CfgVar(CVars.TimeToSleep, value => _islandCfg.TimeToSleep = value);
            CfgVar(CVars.VelocityThreshold, value => _islandCfg.VelocityThreshold = value);
            CfgVar(CVars.Baumgarte, value => _islandCfg.Baumgarte = value);
            CfgVar(CVars.LinearSlop, value => _islandCfg.LinearSlop = value);
            CfgVar(CVars.MaxLinearCorrection, value => _islandCfg.MaxLinearCorrection = value);
            CfgVar(CVars.MaxAngularCorrection, value => _islandCfg.MaxAngularCorrection = value);
            CfgVar(CVars.MaxLinVelocity, value =>
            {
                _maxLinearVelocityRaw = value;
                UpdateMaxLinearVelocity();
            });
            CfgVar(CVars.MaxAngVelocity, value =>
            {
                _maxAngularVelocityRaw = value;
                UpdateMaxAngularVelocity();
            });
            CfgVar(CVars.NetTickrate, value =>
            {
                _tickRate = value;
                UpdateMaxLinearVelocity();
                UpdateMaxAngularVelocity();
            });

            void UpdateMaxLinearVelocity()
            {
                _islandCfg.MaxLinearVelocity = _maxLinearVelocityRaw / _tickRate;
            }

            void UpdateMaxAngularVelocity()
            {
                _islandCfg.MaxAngularVelocity = (MathF.PI * 2 * _maxAngularVelocityRaw) / _tickRate;
            }

            void CfgVar<T>(CVarDef<T> cVar, Action<T> callback) where T : notnull
            {
                _cfg.OnValueChanged(cVar, value =>
                {
                    callback(value);
                    UpdateIslandCfg();
                }, true);
            }
        }

        private void UpdateIslandCfg()
        {
            // OOP bad

            _loneIsland.LoadConfig(_islandCfg);

            foreach (var island in _allocatedIslands)
            {
                island.LoadConfig(_islandCfg);
            }
        }

        public void InitializePools()
        {
            _loneIsland.Clear();
            _activeIslands.Clear();
            _freeIslands.Clear();

            // Check whether allocated islands are sorted
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
                _allocatedIslands.Sort(CapacitySort);
            }

            // TODO: Look at removing islands occasionally just to avoid memory bloat over time.
            // e.g. every 30 seconds go through every island that hasn't been used in 30 seconds and remove it.

            // Free up islands
            foreach (var island in _allocatedIslands)
            {
                island.Clear();
                _freeIslands.Add(island);
            }
        }

        public PhysicsIsland AllocateIsland(int bodyCount, int contactCount, int jointCount)
        {
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
                island.LoadConfig(_islandCfg);
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
