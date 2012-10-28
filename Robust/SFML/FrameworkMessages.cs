using System.Linq;

namespace SFML
{
    /// <summary>
    /// Contains the various message strings for different parts of the custom components of the SFML framework (stuff not natively
    /// in the SFML .NET bindings). Should only ever be used by the custom SFML classes.
    /// This is a replacement of using a resource file since we do not support localizing NetGore's internal messages.
    /// </summary>
    class FrameworkMessages
    {
        public const string BoundingBoxZeroPoints = "You should have at least one point in points";

        public const string BoundingSphereZeroPoints = "You should have at least one point in points.";

        public const string InvalidStringFormat = "Invalid string format. Expected a string in the format \"{0}\".";

        public const string NegativePlaneDistance = "You should specify positive value for {0}.";

        public const string NegativeRadius = "Radius must be greater than 0.";

        public const string NotEnoughCorners = "You have to have at least 8 elements to copy corners.";

        public const string NotEnoughSourceSize = "Source array must be equal or bigger than requested length.";

        public const string NotEnoughTargetSize = "Target array size must be equal or bigger than source array size.";

        public const string NullNotAllowed = "This method does not accept null for this parameter.";

        public const string OppositePlanes =
            "Near plane distance is larger than Far plane distance. Near plane distance must be smaller than Far plane distance.";

        public const string OutRangeFieldOfView = "{0} takes a value between 0 and Pi (180 degrees) in radians.";
    }
}