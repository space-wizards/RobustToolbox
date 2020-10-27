using Robust.Shared.Input;

namespace Robust.Client.Input
{
    /// <summary>
    ///     Contains a helper function for setting up all default engine contexts.
    /// </summary>
    internal static class EngineContexts
    {
        /// <summary>
        ///     Adds the default set of engine contexts to a context container.
        /// </summary>
        /// <param name="contexts">Default contexts will be set up inside this container.</param>
        public static void SetupContexts(IInputContextContainer contexts)
        {
            var common = contexts.GetContext(InputContextContainer.DefaultContextName);
            common.AddFunction(EngineKeyFunctions.Use);
            common.AddFunction(EngineKeyFunctions.UIClick);
            common.AddFunction(EngineKeyFunctions.UIRightClick);

            common.AddFunction(EngineKeyFunctions.CloseModals);
            common.AddFunction(EngineKeyFunctions.EscapeMenu);
            common.AddFunction(EngineKeyFunctions.HideUI);
            common.AddFunction(EngineKeyFunctions.ShowDebugConsole);
            common.AddFunction(EngineKeyFunctions.ShowDebugMonitors);
            common.AddFunction(EngineKeyFunctions.MoveUp);
            common.AddFunction(EngineKeyFunctions.MoveDown);
            common.AddFunction(EngineKeyFunctions.MoveLeft);
            common.AddFunction(EngineKeyFunctions.MoveRight);
            common.AddFunction(EngineKeyFunctions.Walk);
            common.AddFunction(EngineKeyFunctions.CameraRotateRight);
            common.AddFunction(EngineKeyFunctions.CameraRotateLeft);

            common.AddFunction(EngineKeyFunctions.GuiTabNavigateNext);
            common.AddFunction(EngineKeyFunctions.GuiTabNavigatePrev);

            common.AddFunction(EngineKeyFunctions.TextCursorLeft);
            common.AddFunction(EngineKeyFunctions.TextCursorRight);
            common.AddFunction(EngineKeyFunctions.TextCursorWordLeft);
            common.AddFunction(EngineKeyFunctions.TextCursorWordRight);
            common.AddFunction(EngineKeyFunctions.TextCursorBegin);
            common.AddFunction(EngineKeyFunctions.TextCursorEnd);

            common.AddFunction(EngineKeyFunctions.TextCursorSelect);
            common.AddFunction(EngineKeyFunctions.TextCursorSelectLeft);
            common.AddFunction(EngineKeyFunctions.TextCursorSelectRight);
            common.AddFunction(EngineKeyFunctions.TextCursorSelectWordLeft);
            common.AddFunction(EngineKeyFunctions.TextCursorSelectWordRight);
            common.AddFunction(EngineKeyFunctions.TextCursorSelectBegin);
            common.AddFunction(EngineKeyFunctions.TextCursorSelectEnd);

            common.AddFunction(EngineKeyFunctions.TextBackspace);
            common.AddFunction(EngineKeyFunctions.TextSubmit);
            common.AddFunction(EngineKeyFunctions.TextCopy);
            common.AddFunction(EngineKeyFunctions.TextCut);
            common.AddFunction(EngineKeyFunctions.TextPaste);
            common.AddFunction(EngineKeyFunctions.TextSelectAll);
            common.AddFunction(EngineKeyFunctions.TextHistoryPrev);
            common.AddFunction(EngineKeyFunctions.TextHistoryNext);
            common.AddFunction(EngineKeyFunctions.TextReleaseFocus);
            common.AddFunction(EngineKeyFunctions.TextScrollToBottom);
            common.AddFunction(EngineKeyFunctions.TextDelete);

            var editor = contexts.New("editor", common);
            editor.AddFunction(EngineKeyFunctions.EditorLinePlace);
            editor.AddFunction(EngineKeyFunctions.EditorGridPlace);
            editor.AddFunction(EngineKeyFunctions.EditorPlaceObject);
            editor.AddFunction(EngineKeyFunctions.EditorCancelPlace);
            editor.AddFunction(EngineKeyFunctions.EditorRotateObject);

            var human = contexts.New("human", common);
        }
    }
}
