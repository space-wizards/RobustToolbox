namespace Robust.Client
{
    public static class ContentStart
    {
        public static void Start(string[] args)
        {
#if FULL_RELEASE
            throw new System.InvalidOperationException("ContentStart is not available on a full release.");
#else
            GameController.Start(args);
#endif
        }
    }
}
