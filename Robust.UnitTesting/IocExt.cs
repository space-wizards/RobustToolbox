using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Robust.UnitTesting
{
    /// <summary>
    ///     Helpers for setting up IoC in tests.
    /// </summary>
    public static class IocExt
    {
        /// <summary>
        ///     Registers the log manager with test log handler to a dependency collection.
        /// </summary>
        /// <param name="deps">Dependency collection to register into.</param>
        /// <param name="prefix">Prefix for the test log output.</param>
        public static void RegisterLogs(this IDependencyCollection deps, string? prefix = null)
        {
            deps.Register<ILogManager, LogManager>(() =>
            {
                var log = new LogManager();
                log.RootSawmill.AddHandler(new TestLogHandler(prefix));
                return log;
            });
        }
    }
}
