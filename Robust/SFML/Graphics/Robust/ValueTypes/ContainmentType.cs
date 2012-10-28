using System.Linq;

namespace SFML.Graphics
{
    /// <summary>Indicates the extent to which bounding volumes intersect or contain one another.</summary>
    public enum ContainmentType
    {
        /// <summary>
        /// Indicates that one bounding volume completely contains the other.
        /// </summary>
        Disjoint,
        /// <summary>
        /// Indicates there is no overlap between the bounding volumes.
        /// </summary>
        Contains,
        /// <summary>
        /// Indicates that the bounding volumes partially overlap.
        /// </summary>
        Intersects
    }
}