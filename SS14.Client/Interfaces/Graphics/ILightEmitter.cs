using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SS14.Client.Interfaces.Graphics
{
    /// <summary>
    ///     Something which provides a Godot Light 2D.
    /// </summary>
    interface ILightEmitter
    {
        Godot.Light2D Light2D { get; }
    }
}
