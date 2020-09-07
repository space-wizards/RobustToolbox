namespace Robust.Client.Input
{
    public enum KeyEventType : byte
    {
        /// <summary>
        ///     This key is pressed down.
        /// </summary>
        Down,

        /// <summary>
        ///     This key was repeated by the operating system while already being pressed down.
        /// </summary>
        Repeat,

        /// <summary>
        ///     This key has been released.
        /// </summary>
        Up
    }
}
