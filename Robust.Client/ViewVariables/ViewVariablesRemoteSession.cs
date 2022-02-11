namespace Robust.Client.ViewVariables
{
    /// <summary>
    ///     A session allowing the client to read & write on an object on the server.
    /// </summary>
    public sealed class ViewVariablesRemoteSession
    {
        internal uint SessionId { get; }
        public bool Closed { get; internal set; }

        internal ViewVariablesRemoteSession(uint sessionId)
        {
            SessionId = sessionId;
        }
    }
}
