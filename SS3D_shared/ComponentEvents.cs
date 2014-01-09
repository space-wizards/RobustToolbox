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

    public delegate void TestCEventDelegate(TestCEvent ev);
    public class TestCEvent : ComponentEvent
    {
        public TestCEvent(string str)
        {
            testStr = str;
        }

        public string testStr = "thisisatest";
    }
}
