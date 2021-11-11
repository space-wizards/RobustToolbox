using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Log;

namespace Robust.UnitTesting
{
    // ReSharper disable once InconsistentNaming
    [CVarDefs]
    public sealed class RTCVars : CVars
    {
        /*
         * Logging
         */

        /// <summary>
        /// The log level which will cause a test failure.
        /// </summary>
        public static readonly CVarDef<LogLevel> FailureLogLevel =
            CVarDef.Create("robust.tests.failure_log_level", Robust.Shared.Log.LogLevel.Error);
    }
}
