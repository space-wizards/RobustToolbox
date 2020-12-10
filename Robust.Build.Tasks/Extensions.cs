using Microsoft.Build.Framework;

namespace Robust.Build.Tasks
{
    public static class Extensions
    {
        //shamefully copied from avalonia
        public static void LogMessage(this IBuildEngine engine, string message, MessageImportance imp)
        {
            engine.LogMessageEvent(new BuildMessageEventArgs(message, "", "Avalonia", imp));
        }
    }
}
