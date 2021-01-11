namespace Robust.Shared.ContentPack
{
    /// <summary>
    ///     Run levels of the Content entry point.
    /// </summary>
    public enum ModRunLevel: byte
    {
        Error = 0,
        Init = 1,
        PostInit = 2,
        PreInit = 3,
    }
}
