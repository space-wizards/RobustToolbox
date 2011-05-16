using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared;
using Mogre;

using MMOC;

// Helper class for client-side AtomBaseClass-related procedures
// Could be refactored into SS3D_Shared
namespace SS3D.HelperClasses
{
    public class AtomUtil
    {
        /// <summary>
        /// Gets the AtomBaseClass at a screen position given the current camera,
        /// usually used for picking with the mouse.
        /// </summary>
        /// <param name="engine">The ogre engine</param>
        /// <param name="screenPos">The screen position</param>
        /// <param name="worldPos">Outputs the world position of the picked point</param>
        /// <returns>The Atom at the position, or null if none</returns>
        public static AtomBaseClass PickAtScreenPosition(SS3D.Modules.OgreManager engine, Vector2 screenPos, out Vector3 worldPos)
        {
            Ray screenRay = engine.Camera.GetCameraToViewportRay(screenPos.x, screenPos.y);
            RaySceneQuery sceneQuery = engine.SceneMgr.CreateRayQuery(screenRay);
            sceneQuery.QueryTypeMask = Mogre.SceneManager.ENTITY_TYPE_MASK;
            sceneQuery.SetSortByDistance(true);
            RaySceneQueryResult rayResult = sceneQuery.Execute();
            sceneQuery.Dispose();

            if (rayResult.Count == 0)
            {
                worldPos = Vector3.ZERO;
                return null; //Nothing there. The fuck?
            }

            worldPos = screenRay.GetPoint(rayResult.Front.distance);
            return (AtomBaseClass)rayResult.Front.movable.UserObject; //UserObject should ALWAYS be a reference to the object as AtomBaseClass.
        }

        public static AtomBaseClass PickAtScreenPosition(SS3D.Modules.OgreManager mEngine, Vector2 mousePos)
        {

            CollisionTools ct = new CollisionTools(mEngine.SceneMgr);
            CollisionTools.RaycastResult rr = ct.RaycastFromCamera(mEngine.mWindow, mEngine.mCamera, mousePos, Mogre.SceneManager.ENTITY_TYPE_MASK);

            return (AtomBaseClass)rr.Target.UserObject;

        }
    }
}
