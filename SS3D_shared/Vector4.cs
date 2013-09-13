using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared.Serialization;

namespace SS13_Shared
{
    [Serializable]
    public class Vector4
        : INetSerializableType
    {        
        #region Class Variables

        /// <summary>
        /// The X component of the vector
        /// </summary>
        private float x;

        /// <summary>
        /// The Y component of the vector
        /// </summary>
        private float y;

        /// <summary>
        /// The Z component of the vector
        /// </summary>
        private float z;

        /// <summary>
        /// The W component of the vector
        /// </summary>
        private float w;
        #endregion

        #region Constructors

        /// <summary>
        /// Constructor for the Vector3 class accepting three floats
        /// </summary>
        /// <param name="x">The new x value for the Vector3</param>
        /// <param name="y">The new y value for the Vector3</param>
        /// <param name="z">The new z value for the Vector3</param>
        /// <param name="w">The new w value for the Vector3</param>
        /// <implementation>
        /// Uses the mutator properties for the Vector3 components to allow verification of input (if implemented)
        /// This results in the need for pre-initialisation initialisation of the Vector3 components to 0 
        /// Due to the necessity for struct's variables to be set in the constructor before moving control
        /// </implementation>
        public Vector4(float x, float y, float z, float w)
        {
            // Pre-initialisation initialisation
            // Implemented because a struct's variables always have to be set in the constructor before moving control
            this.x = 0;
            this.y = 0;
            this.z = 0;
            this.w = 0;

            // Initialisation
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        /// <summary>
        /// Constructor for the Vector3 class from another Vector3 object
        /// </summary>
        /// <param name="v1">Vector3 representing the new values for the Vector3</param>
        /// <implementation>
        /// Copies values from Vector3 v1 to this vector, does not hold a reference to object v1 
        /// </implementation>
        public Vector4(Vector4 v1)
        {
            // Pre-initialisation initialisation
            // Implemented because a struct's variables always have to be set in the constructor before moving control
            this.x = 0;
            this.y = 0;
            this.z = 0;
            this.w = 0;

            // Initialisation
            X = v1.X;
            Y = v1.Y;
            Z = v1.Z;
            W = v1.W;
        }

        #endregion

        #region Accessors & Mutators

        /// <summary>
        /// Property for the x component of the Vector3
        /// </summary>
        public float X
        {
            get { return x; }
            set { x = value; }
        }

        /// <summary>
        /// Property for the y component of the Vector3
        /// </summary>
        public float Y
        {
            get { return y; }
            set { y = value; }
        }

        /// <summary>
        /// Property for the z component of the Vector3
        /// </summary>
        public float Z
        {
            get { return z; }
            set { z = value; }
        }

        /// <summary>
        /// Property for the w component of the Vector3
        /// </summary>
        public float W
        {
            get { return w; }
            set { w = value; }
        }
        #endregion
    }
}
