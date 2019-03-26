using SS14.Shared.Input;

namespace SS14.Client.Input
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
            common.AddFunction(EngineKeyFunctions.EscapeMenu);
            common.AddFunction(EngineKeyFunctions.FocusChat);
            common.AddFunction(EngineKeyFunctions.ShowDebugMonitors);
            common.AddFunction(EngineKeyFunctions.MoveUp);
            common.AddFunction(EngineKeyFunctions.MoveDown);
            common.AddFunction(EngineKeyFunctions.MoveLeft);
            common.AddFunction(EngineKeyFunctions.MoveRight);
            common.AddFunction(EngineKeyFunctions.Run);

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
