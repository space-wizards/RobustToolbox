using System;

namespace Robust.Client.Graphics
{
    /// <summary>
    ///     Represents a handle to a cursor object.
    /// </summary>
    /// <remarks>
    ///     If disposed while the active cursor, the active cursor will be reset to the default arrow.
    ///
    ///     Note that you cannot dispose standard cursors gotten from <see cref="IClyde.GetStandardCursor"/>.
    /// </remarks>
    /// <seealso cref="IClyde.CreateCursor"/>
    /// <seealso cref="IClyde.GetStandardCursor"/>
    /// <seealso cref="IClyde.SetCursor"/>
    public interface ICursor : IDisposable
    {
    }
}
