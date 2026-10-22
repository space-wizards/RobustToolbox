namespace Robust.Server.ServerStatus;

/// <summary>
/// Helper functions for working with <see cref="IStatusHandlerContext"/>.
/// </summary>
public static class StatusExt
{
    /// <summary>
    /// Add <c>Access-Control-Allow-Origin: *</c> to the response headers for this request.
    /// </summary>
    public static void AddAllowOriginAny(this IStatusHandlerContext context)
    {
        context.ResponseHeaders.Add("Access-Control-Allow-Origin", "*");
    }
}
