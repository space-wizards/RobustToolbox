using System;
using System.Text.Json.Nodes;

namespace Robust.Server.ServerStatus
{
    public interface IStatusHost
    {
        void Start();

        [Obsolete("Use async handlers")]
        void AddHandler(StatusHostHandler handler);
        void AddHandler(StatusHostHandlerAsync handler);

        /// <summary>
        ///     Invoked when a client queries a status request from the server.
        ///     THIS IS INVOKED FROM ANOTHER THREAD.
        ///     I REPEAT, THIS DOES NOT RUN ON THE MAIN THREAD.
        ///     MAKE TRIPLE SURE EVERYTHING IN HERE IS THREAD SAFE DEAR GOD.
        /// </summary>
        event Action<JsonNode> OnStatusRequest;

        /// <summary>
        ///     Invoked when a client queries an info request from the server.
        ///     THIS IS INVOKED FROM ANOTHER THREAD.
        ///     I REPEAT, THIS DOES NOT RUN ON THE MAIN THREAD.
        ///     MAKE TRIPLE SURE EVERYTHING IN HERE IS THREAD SAFE DEAR GOD.
        /// </summary>
        event Action<JsonNode> OnInfoRequest;

        void SetMagicAczProvider(IMagicAczProvider provider);

        /// <summary>
        /// Sets a provider for extra asset files if Hybrid ACZ is available.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If called multiple times, the previous provider is replaced. This only applies if ACZ is ran again later.
        /// <see cref="InvalidateAcz"/> must be called manually for this to have an effect if ACZ has already been built.
        /// </para>
        /// <para>
        /// It is valid to have both a Full Hybrid ACZ provider
        /// and a Magic ACZ provider (via <see cref="SetMagicAczProvider"/>) set at the same time.
        /// The Full Hybrid provider is used if Hybrid ACZ is available, otherwise the Magic ACZ provider is used.
        /// </para>
        /// </remarks>
        /// <param name="provider">The provider to use.</param>
        /// <seealso href="https://docs.spacestation14.com/en/robust-toolbox/acz.html"/>
        void SetFullHybridAczProvider(IFullHybridAczProvider provider);

        /// <summary>
        /// Invalidate the cached ACZ package.
        /// This causes it to be re-generated the next time a client attempts to download the ACZ
        /// (or requests the information for it).
        /// </summary>
        void InvalidateAcz();
    }
}
