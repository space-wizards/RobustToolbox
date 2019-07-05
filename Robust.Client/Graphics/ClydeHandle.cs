namespace Robust.Client.Graphics
{
    internal struct ClydeHandle
    {
        public ClydeHandle(int handle)
        {
            Handle = handle;
        }

        public int Handle { get; }

        public static explicit operator ClydeHandle(int x)
        {
            return new ClydeHandle(x);
        }

        public static explicit operator int(ClydeHandle h)
        {
            return h.Handle;
        }
    }
}
