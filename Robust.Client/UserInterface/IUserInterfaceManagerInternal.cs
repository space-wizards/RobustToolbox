using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Client.UserInterface
{
    internal interface IUserInterfaceManagerInternal : IUserInterfaceManager
    {
        /// <summary>
        ///     Clears and disposes of all UI components.
        ///     Highly destructive!
        /// </summary>
        void DisposeAllComponents();

        void Initialize();
        void InitializeTesting();

        void FrameUpdate(FrameEventArgs args);

        /// <returns>True if a UI control was hit and the key event should not pass through past UI.</returns>
        bool HandleCanFocusDown(Vector2 pointerPosition);

        void HandleCanFocusUp();

        void KeyBindDown(BoundKeyEventArgs args);

        void KeyBindUp(BoundKeyEventArgs args);

        void MouseMove(MouseMoveEventArgs mouseMoveEventArgs);

        void MouseWheel(MouseWheelEventArgs args);

        void TextEntered(TextEventArgs textEvent);

        void ControlHidden(Control control);

        void ControlRemovedFromTree(Control control);

        void RemoveModal(Control modal);

        void Render(IRenderHandle renderHandle);

        void QueueStyleUpdate(Control control);
        void QueueMeasureUpdate(Control control);
        void QueueArrangeUpdate(Control control);
        void CursorChanged(Control control);
        /// <summary>
        /// Hides the tooltip for the indicated control, if tooltip for that control is currently showing.
        /// </summary>
        void HideTooltipFor(Control control);

        /// <summary>
        /// If the control is currently showing a tooltip,
        /// gets the tooltip that was supplied via TooltipSupplier (null if tooltip
        /// was not supplied by tooltip supplier or tooltip is not showing for the control).
        /// </summary>
        Control? GetSuppliedTooltipFor(Control control);
    }
}

