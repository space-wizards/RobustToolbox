using System;
using SS14.Client.UserInterface.CustomControls;

namespace SS14.Client.Console
{
    /// <summary>
    ///     Interface for a chat compatible console.
    /// </summary>
    internal interface IClientChatConsole : IClientConsole
    {
        /// <summary>
        ///     Parses a raw chat message the player has submitted.
        /// </summary>
        /// <param name="text">Raw unsanitized string the player submitted.</param>
        /// <param name="defaultFormat"></param>
        void ParseChatMessage(string text, string defaultFormat = null);

        /// <summary>
        ///     Parses a raw chat message the player has submitted.
        /// </summary>
        void ParseChatMessage(Chatbox chatBox, string text);
    }
}
