using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Robust.Shared.GameObjects
{

    public abstract class SharedLightSensitiveSystem : EntitySystem
    {
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly OccluderSystem _occluder = default!;
        
        protected const float MaxRaycastRange = 100;

        private float _cooldown = 0.1f;
        private TimeSpan _targetTime = TimeSpan.Zero;

        public delegate bool Ignored(EntityUid entity);

        public virtual bool ResolveComp(EntityUid uid, [NotNullWhen(true)] ref LightSensitiveComponent? component)
        {
            if (component is not null)
                return true;

            component = EnsureComp<LightSensitiveComponent>(uid);
            return component != null;
        }

        protected virtual void SetIllumination(EntityUid uid, float value, LightSensitiveComponent? comp = null)
        {
            if(!ResolveComp(uid, ref comp))
                return;
            
            comp.LightLevel = value;
            Dirty(uid, comp);
        }


        public virtual bool InRangeUnOccluded<TState>(MapCoordinates origin, MapCoordinates other, float range,
                TState state, Func<EntityUid, TState, bool> predicate, bool ignoreInsideBlocker = true, IEntityManager? entMan = null)
        {
            if (other.MapId != origin.MapId ||
                other.MapId == MapId.Nullspace) return false;

            var dir = other.Position - origin.Position;
            var length = dir.Length();

            // If range specified also check it
            // TODO: This rounding check is here because the API is kinda eh
            if (range > 0f && length > range + 0.01f) return false;

            if (MathHelper.CloseTo(length, 0)) return true;

            if (length > MaxRaycastRange)
            {
                Log.Warning("InRangeUnOccluded check performed over extreme range. Limiting CollisionRay size.");
                length = MaxRaycastRange;
            }

            var ray = new Ray(origin.Position, dir.Normalized());
            var rayResults = _occluder
                .IntersectRayWithPredicate(origin.MapId, ray, length, state, predicate, false);

            if (rayResults.Count == 0) return true;

            if (!ignoreInsideBlocker) return false;

            foreach (var result in rayResults)
            {
                if (!TryComp(result.HitEntity, out OccluderComponent? o))
                {
                    continue;
                }

                var bBox = o.BoundingBox;
                bBox = bBox.Translated(_transform.GetWorldPosition(result.HitEntity));

                if (bBox.Contains(origin.Position) || bBox.Contains(other.Position))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        public virtual bool InRangeUnOccluded(EntityUid origin, EntityUid other, float range = 3f, Ignored? predicate = null, bool ignoreInsideBlocker = true)
        {

            var originPos = _transform.GetMapCoordinates(origin);
            var otherPos = _transform.GetMapCoordinates(other);

            return InRangeUnOccluded(originPos, otherPos, range, predicate, ignoreInsideBlocker);
        }

        public virtual bool InRangeUnOccluded(MapCoordinates origin, MapCoordinates other, float range, Ignored? predicate, bool ignoreInsideBlocker = true, IEntityManager? entMan = null)
        {
            // No, rider. This is better.
            // ReSharper disable once ConvertToLocalFunction
            var wrapped = (EntityUid uid, Ignored? wrapped)
                => wrapped != null && wrapped(uid);

            return InRangeUnOccluded(origin, other, range, predicate, wrapped, ignoreInsideBlocker, entMan);
        }

    }
}