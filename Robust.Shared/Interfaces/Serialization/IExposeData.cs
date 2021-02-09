using Robust.Shared.Analyzers;
using Robust.Shared.Serialization;

namespace Robust.Shared.Interfaces.Serialization
{
    /// <summary>
    ///     Interface for the "expose data" system, which is basically our method of handling data serialization.
    /// </summary>
    [RequiresExplicitImplementation]
    public interface IExposeData
    {
        /// <summary>
        ///     Will get called to either make this object read data from or provide data to write to/from a serialization format.
        ///     This method should only rely on <paramref name="serializer" /> for reading data.
        ///     External data (i.e. current time) is unreliable as this method will get called to get a representation of this object from some data,
        ///     and may not be called again later.
        /// </summary>
        /// <param name="serializer">
        ///     A serializer that data can be read/written from using its various methods.
        ///     Tell it everything you want to preserve, even your dirtiest secrets.
        /// </param>
        void ExposeData(ObjectSerializer serializer);
    }
}
