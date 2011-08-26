using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared;
using SS3D_shared.HelperClasses;
using SS3D.Atom;

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
        public static AtomBaseClass PickAtScreenPosition(Vector2 screenPos, out Vector2 worldPos)
        {
            /*&Ray screenRay = engine.Camera.GetCameraToViewportRay(screenPos.x, screenPos.y);
            RaySceneQuery sceneQuery = engine.SceneMgr.CreateRayQuery(screenRay);

            sceneQuery.QueryTypeMask = Mogre.SceneManager.ENTITY_TYPE_MASK; //Pick only entities. QueryTypeMask defines the type of object that can be picked up by this ray query.
            sceneQuery.QueryMask = ~QueryFlags.DO_NOT_PICK; //Do not pick stuff with the DO_NOT_PICK Flag. QueryMask is for cutom flags. For example a mob flag. With that it would pick mobs.
                                                            //They're defined in QueryFlags.cs in the helper classes.

            sceneQuery.SetSortByDistance(true);
            RaySceneQueryResult rayResult = sceneQuery.Execute();
            sceneQuery.Dispose();

            if (rayResult.Count == 0)
            {
                worldPos = Mogre.Vector3.ZERO;
                return null;
            }

            worldPos = screenRay.GetPoint(rayResult.Front.distance);
            return (AtomBaseClass)rayResult.Front.movable.UserObject; //UserObject should ALWAYS be a reference to the object as AtomBaseClass.*/
            worldPos = new Vector2(0, 0);
            return null;
        
        }

        public static Atom.Atom PickAtScreenPosition(Vector2 mousePos)
        {

            /*CollisionTools ct = new CollisionTools(mEngine.SceneMgr);
            CollisionTools.RaycastResult rr = ct.RaycastFromCamera(mEngine.mWindow, mEngine.mCamera, mousePos, QueryFlags.ENTITY_ATOM); //Mogre.SceneManager.ENTITY_TYPE_MASK does not go here!!!
                                                                                                                                                     //The flags in that position are for the QueryMask
                                                                                                                                                     //Not the QueryTypeMask. The QueryMask is for custom flags.
                                                                                                                                                     //Mogre.SceneManager.ENTITY_TYPE_MASK is a QueryTypeMask Flag.
                                                                                                                                                     //This will probably cause problems at some point if it stays like this.
                                                                                                                                                     //See the Method above if you don't understand this.
            if (rr != null)
            {
                return (Atom.Atom)rr.Target.UserObject;
            }*/

            return null;

        }
    }
}
