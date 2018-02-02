using SS14.Shared.GameObjects;

namespace SS14.Shared.GameObjects
{
    public class MouseClickType
    {
        public static Clicktype ConvertComponentMessageTypeToClickType(ComponentMessageType type)
        {
            Clicktype result = 0;
            switch (type)
            {
                case ComponentMessageType.LeftClick:
                    result = Clicktype.Left;
                    break;
                case ComponentMessageType.RightClick:
                    result = Clicktype.Right;
                    break;
                case ComponentMessageType.AltLeftClick:
                    result = Clicktype.LeftAlt;
                    break;
                case ComponentMessageType.AltRightClick:
                    result = Clicktype.RightAlt;
                    break;
                case ComponentMessageType.ShiftLeftClick:
                    result = Clicktype.LeftShift;
                    break;
                case ComponentMessageType.ShiftRightClick:
                    result = Clicktype.RightShift;
                    break;
                case ComponentMessageType.CtrlLeftClick:
                    result = Clicktype.LeftCtrl;
                    break;
                case ComponentMessageType.CtrlRightClick:
                    result = Clicktype.RightCtrl;
                    break;
            }
            return result;
        }

        public static ComponentMessageType ConvertClickTypeToComponentMessageType(Clicktype clickType)
        {
            ComponentMessageType result = ComponentMessageType.Null;
            switch(clickType)
            {
                case Clicktype.Left:
                    result = ComponentMessageType.LeftClick;
                    break;
                case Clicktype.Right:
                    result = ComponentMessageType.RightClick;
                    break;
                case Clicktype.LeftAlt:
                    result = ComponentMessageType.AltLeftClick;
                    break;
                case Clicktype.RightAlt:
                    result = ComponentMessageType.AltRightClick;
                    break;
                case Clicktype.LeftShift:
                    result = ComponentMessageType.ShiftLeftClick;
                    break;
                case Clicktype.RightShift:
                    result = ComponentMessageType.ShiftRightClick;
                    break;
                case Clicktype.LeftCtrl:
                    result = ComponentMessageType.CtrlLeftClick;
                    break;
                case Clicktype.RightCtrl:
                    result = ComponentMessageType.CtrlRightClick;
                    break;

            }
            return result;
        }
    }

    public enum Clicktype
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
