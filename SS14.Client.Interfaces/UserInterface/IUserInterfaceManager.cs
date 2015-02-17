using SFML.Window;
using Lidgren.Network;
using SS14.Client.Interfaces.GOC;
using SS14.Shared;

namespace SS14.Client.Interfaces.UserInterface
{
    public interface IUserInterfaceManager
    {
        IDragDropInfo DragInfo { get; }
        IPlayerAction currentTargetingAction { get; }

        void AddComponent(IGuiComponent component);
        void RemoveComponent(IGuiComponent component);
        void ComponentUpdate(GuiComponentType type, params object[] args);
        void DisposeAllComponents();
        void DisposeAllComponents<T>();
        void ResizeComponents();
        void SetFocus(IGuiComponent newFocus);
        void RemoveFocus();
        void Update(float frameTime);
        void Render();

        void StartTargeting(IPlayerAction action);
        void SelectTarget(object target);
        void CancelTargeting();

        void ToggleMoveMode();

        bool KeyDown(KeyEventArgs e);
        void MouseWheelMove(MouseWheelEventArgs e);
        void MouseMove(MouseMoveEventArgs e);
        bool MouseUp(MouseButtonEventArgs e);
        bool MouseDown(MouseButtonEventArgs e);

        void HandleNetMessage(NetIncomingMessage msg);
    }
}