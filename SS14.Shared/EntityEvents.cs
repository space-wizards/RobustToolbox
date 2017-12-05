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
        public TestCEventArgs(string str)
        {
            TestStr = str;
        }

        public String TestStr { get; set; }
    }

    public class ClickedOnEntityEventArgs : EntityEventArgs
    {
        public int Clicker { get; set; }
        public int Clicked { get; set; }
        public int MouseButton { get; set; }
    }

}
