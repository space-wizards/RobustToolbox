using System;

namespace SS14.Shared
{
    public interface IEntityEventSubscriber
    { }

    //public delegate void ComponentEventHandler(object sender, IComponentEventArgs ev);
    public delegate void EntityEventHandler<in T>(object sender, T ev) where T : EntityEventArgs;

    public class EntityEventArgs : EventArgs
    {
    }

    //MAKE SURE YOU SET UP YOUR SHIT LIKE THIS SO WE DONT END UP WITH A HUGE MESS.
    public delegate void TestCEventDelegate(TestCEvent ev);
    public class TestCEvent : EntityEventArgs
    {
        public string testStr = "thisisatest";

        public TestCEvent(string str)
        {
            testStr = str;
        }
    }

    public class ClickedOnEntityEventArgs : EntityEventArgs
    {
        public int Clicker;
        public int Clicked;
        public int MouseButton;
    }

}
