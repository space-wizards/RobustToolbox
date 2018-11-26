using System;
using Newtonsoft.Json.Linq;

namespace SS14.Server.Interfaces.ServerStatus
{
    public interface IStatusHost
    {
        void Start();

        /// <summary>
        ///     Invoked when a client queries a status request from the server.
        ///     THIS IS INVOKED FROM ANOTHER THREAD.
        ///     I REPEAT, THIS DOES NOT RUN ON THE MAIN THREAD.
        ///     MAKE TRIPLE SURE EVERYTHING IN HERE IS THREAD SAFE DEAR GOD.
        /// </summary>
        event Action<JObject> OnStatusRequest;
    }
}
