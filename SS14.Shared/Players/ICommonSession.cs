using SS14.Shared.Enums;

namespace SS14.Shared.Players
{
    /// <summary>
    ///     Common info between client and server sessions.
    /// </summary>
    public interface ICommonSession : IBaseSession
    {
        /// <summary>
        ///     Status of the session.
        /// </summary>
        SessionStatus Status { get; set; }
    }
}
