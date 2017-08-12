using SS14.Shared.GameObjects;

namespace SS14.Shared.GameObjects
{
    public class MouseClickType
    {
        public const int None = 0;
        public const int Left = 1;
        public const int Right = 2;
        public const int LeftAlt = 3;
        public const int RightAlt = 4;
        public const int LeftShift = 5;
        public const int RightShift = 6;
        public const int LeftCtrl = 7;
        public const int RightCtrl = 8;

        public static int ConvertComponentMessageTypeToClickType(ComponentMessageType type)
        {
            int result = 0;
            switch (type)
            {
                case ComponentMessageType.LeftClick:
                    result = MouseClickType.Left;
                    break;
                case ComponentMessageType.RightClick:
                    result = MouseClickType.Right;
                    break;
                case ComponentMessageType.AltLeftClick:
                    result = MouseClickType.LeftAlt;
                    break;
                case ComponentMessageType.AltRightClick:
                    result = MouseClickType.RightAlt;
                    break;
                case ComponentMessageType.ShiftLeftClick:
                    result = MouseClickType.LeftShift;
                    break;
                case ComponentMessageType.ShiftRightClick:
                    result = MouseClickType.RightShift;
                    break;
                case ComponentMessageType.CtrlLeftClick:
                    result = MouseClickType.LeftCtrl;
                    break;
                case ComponentMessageType.CtrlRightClick:
                    result = MouseClickType.RightCtrl;
                    break;
            }
            return result;
        }

        public static ComponentMessageType ConvertClickTypeToComponentMessageType(int clickType)
        {
            ComponentMessageType result = ComponentMessageType.Null;
            switch(clickType)
            {
                case MouseClickType.Left:
                    result = ComponentMessageType.LeftClick;
                    break;
                case MouseClickType.Right:
                    result = ComponentMessageType.RightClick;
                    break;
                case MouseClickType.LeftAlt:
                    result = ComponentMessageType.AltLeftClick;
                    break;
                case MouseClickType.RightAlt:
                    result = ComponentMessageType.AltRightClick;
                    break;
                case MouseClickType.LeftShift:
                    result = ComponentMessageType.ShiftLeftClick;
                    break;
                case MouseClickType.RightShift:
                    result = ComponentMessageType.ShiftRightClick;
                    break;
                case MouseClickType.LeftCtrl:
                    result = ComponentMessageType.CtrlLeftClick;
                    break;
                case MouseClickType.RightCtrl:
                    result = ComponentMessageType.CtrlRightClick;
                    break;

            }
            return result;
        }
    }



}
