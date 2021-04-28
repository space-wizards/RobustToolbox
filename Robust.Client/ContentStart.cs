namespace Robust.Client
{
    public static class ContentStart
    {
        public static void Start(string[] args)
        {
#if FULL_RELEASE
            throw new System.InvalidOperationException("ContentStart.Start is not available on a full release.");
#else
            GameController.Start(args, true);
#endif
        }

        public static void StartLibrary(string[] args, GameControllerOptions options)
        {
            GameController.Start(args, true, null, options);
        }
    }
}
