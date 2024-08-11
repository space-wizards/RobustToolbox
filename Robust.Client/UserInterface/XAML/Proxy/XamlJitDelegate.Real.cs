#if TOOLS
using System;
using System.Reflection;

namespace Robust.Client.UserInterface.XAML.Proxy;

/// <summary>
/// This callback has the approximate type of XamlJitCompiler.CompilePopulate.
///
/// However, it signals errors differently. Implementors of this delegate should
/// inform the users of errors in their own way.
///
/// (Hot-reloading should generally not take down the process, so please don't just
/// rethrow the errors.)
/// </summary>
delegate MethodInfo? XamlJitDelegate(Type type, Uri uri, string filename, string content);
#endif
