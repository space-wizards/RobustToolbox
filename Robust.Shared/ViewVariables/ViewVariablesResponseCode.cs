namespace Robust.Shared.ViewVariables;

public enum ViewVariablesResponseCode : ushort
{
    /// <summary>
    ///     Request went through successfully.
    /// </summary>
    Ok = 200,

    /// <summary>
    ///     Request was invalid or something.
    /// </summary>
    InvalidRequest = 400,

    /// <summary>
    ///     Come back with admin access.
    /// </summary>
    NoAccess = 401,

    /// <summary>
    ///     Object pointing to by the selector does not exist.
    /// </summary>
    NoObject = 404,
}
