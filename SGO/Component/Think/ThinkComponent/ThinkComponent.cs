using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SGO.Component.Think.ThinkComponent
{
    public class ThinkComponent : IThinkComponent
    {
        public virtual void OnBump(object sender, params object[] list)
        {

        }

        public virtual void Update(float frameTime)
        { }
    }
}
