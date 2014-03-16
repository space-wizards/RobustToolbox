using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS13_Shared
{
    public interface IComponentEventSubscriber
    {}

    //public delegate void ComponentEventHandler(object sender, IComponentEventArgs ev);
    public delegate void ComponentEventHandler<in T>(object sender, T ev) where T:ComponentEventArgs;

    public interface IComponentEventArgs
    {
    }

    public class ComponentEventArgs:EventArgs, IComponentEventArgs
    {
    }

    //MAKE SURE YOU SET UP YOUR SHIT LIKE THIS SO WE DONT END UP WITH A HUGE MESS.
    public delegate void TestCEventDelegate(TestCEvent ev);
    public class TestCEvent : ComponentEventArgs
    {
        public string testStr = "thisisatest";

        public TestCEvent(string str)
        {
            testStr = str;
        }
    }

    public class ClickedOnEntityEventArgs : ComponentEventArgs
    {
        public int Clicker;
        public int Clicked;
    }
}
