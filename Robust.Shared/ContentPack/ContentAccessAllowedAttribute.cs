using System;
using Robust.Shared.IoC;

namespace Robust.Shared.ContentPack;

// Yes this attribute is a crappy workaround because I cba to automatically infer this
// based on constructors or something. I'm lazy and want to code editor dockers, deal with it.

/// <summary>
/// Mark this type as content safe, which means <see cref="ModLoaderExt.IsContentTypeAccessAllowed"/> will pass for it.
/// This means it can be spawned dynamically with <see cref="IDynamicTypeFactory"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal sealed class ContentAccessAllowedAttribute : Attribute
{

}
