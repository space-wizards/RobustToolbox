using System;
using System.Runtime.InteropServices;
using System.Security;
using SFML.Window;

namespace SFML
{
    namespace Graphics
    {
        ////////////////////////////////////////////////////////////
        /// <summary>
        /// Abstract base class for render targets (renderwindow, renderimage)
        /// </summary>
        ////////////////////////////////////////////////////////////
        public interface RenderTarget
        {
            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Size of the rendering region of the target
            /// </summary>
            ////////////////////////////////////////////////////////////
            Vector2u Size { get; }

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Default view of the target
            /// </summary>
            ////////////////////////////////////////////////////////////
            View DefaultView {get;}

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Return the current active view
            /// </summary>
            /// <returns>The current view</returns>
            ////////////////////////////////////////////////////////////
            View GetView();

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Change the current active view
            /// </summary>
            /// <param name="view">New view</param>
            ////////////////////////////////////////////////////////////
            void SetView(View view);

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Get the viewport of a view applied to this target
            /// </summary>
            /// <param name="view">Target view</param>
            /// <returns>Viewport rectangle, expressed in pixels in the current target</returns>
            ////////////////////////////////////////////////////////////
            IntRect GetViewport(View view);

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Convert a point in target coordinates into view coordinates
            /// This version uses the current view of the target
            /// </summary>
            /// <param name="point">Point to convert, relative to the target</param>
            /// <returns>Converted point</returns>
            ////////////////////////////////////////////////////////////
            Vector2 ConvertCoords(Vector2i point);

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Convert a point in target coordinates into view coordinates
            /// This version uses the given view
            /// </summary>
            /// <param name="point">Point to convert, relative to the target</param>
            /// <param name="view">Target view to convert the point to</param>
            /// <returns>Converted point</returns>
            ////////////////////////////////////////////////////////////
            Vector2 ConvertCoords(Vector2i point, View view);

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Clear the entire target with black color
            /// </summary>
            ////////////////////////////////////////////////////////////
            void Clear();

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Clear the entire target with a single color
            /// </summary>
            /// <param name="color">Color to use to clear the window</param>
            ////////////////////////////////////////////////////////////
            void Clear(Color color);

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Draw a drawable object to the render-target, with default render states
            /// </summary>
            /// <param name="drawable">Object to draw</param>
            ////////////////////////////////////////////////////////////
            void Draw(Drawable drawable);

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Draw a drawable object to the render-target
            /// </summary>
            /// <param name="drawable">Object to draw</param>
            /// <param name="states">Render states to use for drawing</param>
            ////////////////////////////////////////////////////////////
            void Draw(Drawable drawable, RenderStates states);

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Draw primitives defined by an array of vertices, with default render states
            /// </summary>
            /// <param name="vertices">Array of vertices to draw</param>
            /// <param name="type">Type of primitives to draw</param>
            ////////////////////////////////////////////////////////////
            void Draw(Vertex[] vertices, PrimitiveType type);

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Draw primitives defined by an array of vertices
            /// </summary>
            /// <param name="vertices">Array of vertices to draw</param>
            /// <param name="type">Type of primitives to draw</param>
            /// <param name="states">Render states to use for drawing</param>
            ////////////////////////////////////////////////////////////
            void Draw(Vertex[] vertices, PrimitiveType type, RenderStates states);

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Draw primitives defined by a sub-array of vertices, with default render states
            /// </summary>
            /// <param name="vertices">Array of vertices to draw</param>
            /// <param name="start">Index of the first vertex to draw in the array</param>
            /// <param name="count">Number of vertices to draw</param>
            /// <param name="type">Type of primitives to draw</param>
            ////////////////////////////////////////////////////////////
            void Draw(Vertex[] vertices, uint start, uint count, PrimitiveType type);

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Draw primitives defined by a sub-array of vertices
            /// </summary>
            /// <param name="vertices">Pointer to the vertices</param>
            /// <param name="start">Index of the first vertex to use in the array</param>
            /// <param name="count">Number of vertices to draw</param>
            /// <param name="type">Type of primitives to draw</param>
            /// <param name="states">Render states to use for drawing</param>
            ////////////////////////////////////////////////////////////
            void Draw(Vertex[] vertices, uint start, uint count, PrimitiveType type, RenderStates states);

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Save the current OpenGL render states and matrices.
            ///
            /// This function can be used when you mix SFML drawing
            /// and direct OpenGL rendering. Combined with PopGLStates,
            /// it ensures that:
            /// \li SFML's internal states are not messed up by your OpenGL code
            /// \li your OpenGL states are not modified by a call to a SFML function
            ///
            /// More specifically, it must be used around code that
            /// calls Draw functions. Example:
            ///
            /// // OpenGL code here...
            /// window.PushGLStates();
            /// window.Draw(...);
            /// window.Draw(...);
            /// window.PopGLStates();
            /// // OpenGL code here...
            ///
            /// Note that this function is quite expensive: it saves all the
            /// possible OpenGL states and matrices, even the ones you
            /// don't care about. Therefore it should be used wisely.
            /// It is provided for convenience, but the best results will
            /// be achieved if you handle OpenGL states yourself (because
            /// you know which states have really changed, and need to be
            /// saved and restored). Take a look at the ResetGLStates
            /// function if you do so.
            /// </summary>
            ////////////////////////////////////////////////////////////
            void PushGLStates();

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Restore the previously saved OpenGL render states and matrices.
            ///
            /// See the description of PushGLStates to get a detailed
            /// description of these functions.
            /// </summary>
            ////////////////////////////////////////////////////////////
            void PopGLStates();

            ////////////////////////////////////////////////////////////
            /// <summary>
            /// Reset the internal OpenGL states so that the target is ready for drawing.
            ///
            /// This function can be used when you mix SFML drawing
            /// and direct OpenGL rendering, if you choose not to use
            /// PushGLStates/PopGLStates. It makes sure that all OpenGL
            /// states needed by SFML are set, so that subsequent Draw()
            /// calls will work as expected.
            ///
            /// Example:
            ///
            /// // OpenGL code here...
            /// glPushAttrib(...);
            /// window.ResetGLStates();
            /// window.Draw(...);
            /// window.Draw(...);
            /// glPopAttrib(...);
            /// // OpenGL code here...
            /// </summary>
            ////////////////////////////////////////////////////////////
            void ResetGLStates();
        }
    }
}
