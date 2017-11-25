using System;

namespace SS14.Shared
{
    public interface IEntityEventSubscriber
    { }
    
    public delegate void EntityEventHandler<in T>(object sender, T ev) where T : EntityEventArgs;

    public class EntityEventArgs : EventArgs
    {
    }

    //MAKE SURE YOU SET UP YOUR SHIT LIKE THIS SO WE DONT END UP WITH A HUGE MESS.
    public delegate void TestCEventDelegate(TestCEventArgs ev);
    public class TestCEventArgs : EntityEventArgs
    {
        private string testStr;

        public TestCEventArgs(string str)
        {
            TestStr = str;
        }

        public global::System.String TestStr { get => testStr; set => testStr = value; }
    }

    public class ClickedOnEntityEventArgs : EntityEventArgs
    {
        private int clicker;
        private int clicked;
        private int mouseButton;

        public global::System.Int32 Clicker { get => clicker; set => clicker = value; }
        public global::System.Int32 Clicked { get => clicked; set => clicked = value; }
        public global::System.Int32 MouseButton { get => mouseButton; set => mouseButton = value; }
    }

}
