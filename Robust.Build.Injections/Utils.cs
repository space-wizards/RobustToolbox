using Microsoft.Build.Framework;

namespace Robust.Build.Injections
{
    public static class Utils
    {
        public static void LogMessage(this IBuildEngine buildEngine, string message, MessageImportance importance)
        {
            var e = new BuildMessageEventArgs(message, "", "Robust.Build.Injections", importance);
            buildEngine.LogMessageEvent(e);
        }

        public static void LogError(this IBuildEngine buildEngine, string category, string message, string location)
        {
            var e = new BuildErrorEventArgs(category, "", location, 0, 0, 0, 0, message, "",
                "Robust.Build.Injections");
            buildEngine.LogErrorEvent(e);
        }

        public static void LogWarning(this IBuildEngine buildEngine, string category, string message, string location)
        {
            var e = new BuildWarningEventArgs(category, "", location, 0, 0, 0, 0, message, "",
                "Robust.Build.Injections");
            buildEngine.LogWarningEvent(e);
        }
    }
}
