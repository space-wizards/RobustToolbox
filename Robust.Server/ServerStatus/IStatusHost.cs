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

        /// <summary>
        /// Set information used by automatic-client-zipping to determine the layout of your dev setup,
        /// and which assembly files to send.
        /// </summary>
        /// <param name="clientBinFolder">
        /// The name of your client project in the bin/ folder on the top of your project.
        /// </param>
        /// <param name="clientAssemblyNames">
        /// The list of client assemblies to send from the aforementioned folder.
        /// </param>
        void SetAczInfo(string clientBinFolder, string[] clientAssemblyNames);
    }
}
