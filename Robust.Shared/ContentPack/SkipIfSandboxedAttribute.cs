using System;

namespace Robust.Shared.ContentPack;

/// <summary>
/// If defined on an assembly, causes Robust to skip trying to load this assembly if running on sandboxed mode.
/// </summary>
/// <remarks>
/// If you have some sandbox-unsafe code you want to optionally enable for your project,
/// you can put the code in a leaf project,
/// tag the assembly with this attribute,
/// and then detect at runtime whether the assembly was loaded to enable these features.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class SkipIfSandboxedAttribute : Attribute
{

}
