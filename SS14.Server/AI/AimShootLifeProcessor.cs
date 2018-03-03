using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Server.GameObjects;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Physics;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace SS14.Server.AI
{
    /// <summary>
    ///     The object stays stationary. The object will periodically scan for *any* life forms in its radius, and engage them.
    ///     The object will rotate itself to point at the locked entity, and if it has a weapon will shoot at the entity.
    /// </summary>
    [AiLogicProcessor("AimShootLife")]
    class AimShootLifeProcessor : AiLogicProcessor
    {
        private readonly ICollisionManager _physMan;
        private readonly IServerEntityManager _entMan;

        private List<IEntity> _workList = new List<IEntity>();

        private const float ScanPeriod = 0.5f; // tweak this for performance and gameplay experience
        private float _lastScan;
        
        private IEntity _curTarget;

        public AimShootLifeProcessor()
        {
            _physMan = IoCManager.Resolve<ICollisionManager>();
            _entMan = IoCManager.Resolve<IServerEntityManager>();
        }

        public override void Update(float frameTime)
        {
            if(SelfEntity == null)
                return;
            
            DoScanning(frameTime);
            DoTracking(frameTime);
        }

        private void DoScanning(float frameTime)
        {
            if (frameTime - _lastScan > ScanPeriod)
            {
                _lastScan = frameTime;
                _curTarget = FindBestTarget();
            }
        }

        private void DoTracking(float frameTime)
        {
            // not valid entity to target.
            if (_curTarget == null || !_curTarget.IsValid())
            {
                _curTarget = null;
                return;
            }

            // point me at the target
            var tarPos = _curTarget.GetComponent<ITransformComponent>().WorldPosition;
            var myPos = SelfEntity.GetComponent<ITransformComponent>().WorldPosition;
            var dir = (tarPos - myPos).Normalized;

            SelfEntity.GetComponent<IServerTransformComponent>().LocalRotation = dir.GetDir().ToAngle();

            //TODO: shoot gun if i have it
        }

        private IEntity FindBestTarget()
        {
            // "best" target is the closest one with LOS

            var ents = _entMan.GetEntitiesInRange(SelfEntity, VisionRadius);
            var myTransform = SelfEntity.GetComponent<IServerTransformComponent>();
            var maxRayLen = VisionRadius * 2.5f; // circle inscribed in square, square diagonal = 2*r*sqrt(2)

            _workList.Clear();
            foreach (var entity in ents)
            {
                // filter to "people" entities (entities with controllers)
                if (!entity.HasComponent<IMoverComponent>())
                    continue;

                // build the ray
                var dir = entity.GetComponent<TransformComponent>().WorldPosition - myTransform.WorldPosition;
                var ray = new Ray(myTransform.WorldPosition, dir);

                // cast the ray
                var result = _physMan.IntersectRay(ray, maxRayLen);

                // add to visible list
                if (result.HitEntity == entity)
                    _workList.Add(entity);
            }
            
            // get closest entity in list
            var closestEnt = GetClosest(myTransform.WorldPosition, _workList);

            // return closest
            return closestEnt;
        }

        private static IEntity GetClosest(Vector2 origin, IEnumerable<IEntity> list)
        {
            IEntity closest = null;
            var minDistSqrd = float.PositiveInfinity;

            foreach (var ent in list)
            {
                var pos = ent.GetComponent<ITransformComponent>().WorldPosition;
                var distSqrd = (pos - origin).LengthSquared;

                if (distSqrd < minDistSqrd)
                    continue;

                closest = ent;
                minDistSqrd = distSqrd;
            }

            return closest;
        }
    }
}
