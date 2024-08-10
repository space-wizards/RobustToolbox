using System;
using System.Reflection;

namespace Robust.Client.UserInterface.XAML.Proxy;

delegate MethodInfo? XamlJitDelegate(Type type, Uri uri, string filename, string content);
