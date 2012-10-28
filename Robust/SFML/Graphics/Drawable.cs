using System;

namespace SFML
{
    namespace Graphics
    {
        ////////////////////////////////////////////////////////////
        /// <summary>
        /// Interface for every object that can be drawn to a render window
        /// </summary>
        ////////////////////////////////////////////////////////////
        public interface Drawable
        {
            ////////////////////////////////////////////////////////////
            /// <summmary>
            /// Draw the object to a render target
            ///
            /// This is a pure virtual function that has to be implemented
            /// by the derived class to define how the drawable should be
            /// drawn.
            /// </summmary>
            /// <param name="target">Render target to draw to</param>
            /// <param name="states">Current render states</param>
            ////////////////////////////////////////////////////////////
            void Draw(RenderTarget target, RenderStates states);
        }
    }
}
