using System.Diagnostics.CodeAnalysis;
using Robust.Shared.ComponentTrees;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Handles the calculations of light sensitivity for entities with the <see cref="LightSensitiveComponent"/>.
    ///     Due to the potential performance impact of calculating the illumination of an unspecified number of entities of varying importance and tick rates,
    ///     this system will not be enabled by default and even when enabled will not execute until entites exist with the corresponding Component and
    ///     specifically request updates.
    ///     I did my best to optimize this but use and implement cautiously.
    /// </summary>
    public abstract class SharedLightSensitiveSystem : EntitySystem
    {
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] protected readonly OccluderSystem Occluder = default!;
        [Dependency] protected readonly SharedLightTreeSystem LightTree = default!;

        public virtual bool ResolveComp(EntityUid uid, [NotNullWhen(true)] ref LightSensitiveComponent? component)
        {
            if (component is not null)
                return true;

            component = EnsureComp<LightSensitiveComponent>(uid);
            return component != null;
        }

        protected virtual void SetIllumination(EntityUid uid, float value, LightSensitiveComponent? comp = null)
        {
            if (!ResolveComp(uid, ref comp))
                return;

            if (comp.LightLevel != value)
            {
                comp.LightLevel = value;
                Dirty(uid, comp);
            }
        }

        /// <summary>
        /// For use with masks and directional lights. We need the angle from the direction the PointLightComponent is facing to the target
        /// </summary>
        /// <returns>The <see cref="Angle"/> between the light and the target. In the event the light is parented to the target,
        /// (i.e. the angle of a flashlight to the person holding it) the default angle is 0</returns>
        public Angle GetAngle(EntityUid lightUid, TransformComponent lightXform, SharedPointLightComponent lightComp, EntityUid targetUid, TransformComponent targetXform)
        {
            var (lightPos, lightRot) = _transform.GetWorldPositionRotation(lightXform);
            lightPos += lightRot.RotateVec(lightComp.Offset);

            var (targetPos, targetRot) = _transform.GetWorldPositionRotation(targetXform);

            var mapDiff = targetPos - lightPos;

            var oppositeMapDiff = (-lightRot).RotateVec(mapDiff);
            var angle = oppositeMapDiff.ToWorldAngle();

            if (angle == double.NaN && _transform.ContainsEntity(targetUid, lightUid) || _transform.ContainsEntity(lightUid, targetUid))
            {
                angle = 0f;
            }

            return angle;
        }

        // TODO calculate mask coefficient from the image itself to multiply against the light value of the PointLight
        // public float ApplyMask()
        // {

        // }

    }
}
