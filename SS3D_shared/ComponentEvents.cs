using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SS13_Shared
{
    public delegate void ComponentEventDelegate(ComponentEvent ev);
    public class ComponentEvent
    {
    }

    //MAKE SURE YOU SET UP YOUR SHIT LIKE THIS SO WE DONT END UP WITH A HUGE MESS.
    public delegate void TestCEventDelegate(TestCEvent ev);
    public class TestCEvent : ComponentEvent
    {
        public string testStr = "thisisatest";

        public TestCEvent(string str)
        {
            testStr = str;
        }
    }
}
