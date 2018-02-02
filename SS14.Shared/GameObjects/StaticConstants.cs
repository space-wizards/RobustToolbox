using SS14.Shared.GameObjects;

namespace SS14.Shared.GameObjects
{
    public class MouseClickType
    {
        public static ClickType ConvertComponentMessageTypeToClickType(ComponentMessageType type)
        {
            ClickType result = 0;
            switch (type)
            {
                case ComponentMessageType.LeftClick:
                    result = ClickType.Left;
                    break;
                case ComponentMessageType.RightClick:
                    result = ClickType.Right;
                    break;
                case ComponentMessageType.AltLeftClick:
                    result = ClickType.LeftAlt;
                    break;
                case ComponentMessageType.AltRightClick:
                    result = ClickType.RightAlt;
                    break;
                case ComponentMessageType.ShiftLeftClick:
                    result = ClickType.LeftShift;
                    break;
                case ComponentMessageType.ShiftRightClick:
                    result = ClickType.RightShift;
                    break;
                case ComponentMessageType.CtrlLeftClick:
                    result = ClickType.LeftCtrl;
                    break;
                case ComponentMessageType.CtrlRightClick:
                    result = ClickType.RightCtrl;
                    break;
            }
            return result;
        }

        public static ComponentMessageType ConvertClickTypeToComponentMessageType(ClickType clickType)
        {
            ComponentMessageType result = ComponentMessageType.Null;
            switch(clickType)
            {
                case ClickType.Left:
                    result = ComponentMessageType.LeftClick;
                    break;
                case ClickType.Right:
                    result = ComponentMessageType.RightClick;
                    break;
                case ClickType.LeftAlt:
                    result = ComponentMessageType.AltLeftClick;
                    break;
                case ClickType.RightAlt:
                    result = ComponentMessageType.AltRightClick;
                    break;
                case ClickType.LeftShift:
                    result = ComponentMessageType.ShiftLeftClick;
                    break;
                case ClickType.RightShift:
                    result = ComponentMessageType.ShiftRightClick;
                    break;
                case ClickType.LeftCtrl:
                    result = ComponentMessageType.CtrlLeftClick;
                    break;
                case ClickType.RightCtrl:
                    result = ComponentMessageType.CtrlRightClick;
                    break;

            }
            return result;
        }
    }

    public enum ClickType
    {
        None = 0,
        Left = 1,
        Right = 2,
        LeftAlt = 3,
        RightAlt = 4,
        LeftShift = 5,
        RightShift = 6,
        LeftCtrl = 7,
        RightCtrl = 8
    }
}
