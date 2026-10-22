using Microsoft.Build.Framework;

namespace Robust.Xaml
{
    /// <summary>
    /// Taken from https://github.com/AvaloniaUI/Avalonia/blob/c85fa2b9977d251a31886c2534613b4730fbaeaf/src/Avalonia.Build.Tasks/Extensions.cs
    /// </summary>
    internal static class Extensions
    {
        //shamefully copied from avalonia
        public static void LogMessage(this IBuildEngine engine, string message, MessageImportance imp)
        {
            engine.LogMessageEvent(new BuildMessageEventArgs(message, "", "Avalonia", imp));
        }
    }
}
