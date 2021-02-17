# XamlUI notes about Rider

XAML support in both Rider and VS is pretty hardcoded for all supported frameworks. Rider seems less bad about this but they still hardcode a lot of type paths and do other silly stuff. Luckily, they support Avalonia. So... let's pretend to be Avalonia!

**JetBrains, if you're reading this, please don't sue me or take away our licenses pls I only reverse engineered your program to avoid wasting time on your issue tracker.**

## Where to find this info

The primary XAML support in Rider appears to be in `lib/ReSharperHost/JetBrains.ReSharper.Psi.Xaml.dll`. I recommend you decompile it with DotPeek to a project for navigating around. You can then look around the decompiled code (it's not obfuscated at all) to figure out how Rider does all this stuff.

**JetBrains, if you're reading this, please don't sue me or take away our licenses pls I only reverse engineered your program to avoid wasting time on your issue tracker.**

## Attributes

Attributes like `XmlnsAttributionAttribute` are hardcoded for full name with namespace. That's also where it stops, so we just define our own `Avalonia.Metadata.XmlnsDefinitionAttribute` and this fools Rider into recognizing it and working. Hooray!

## `http://schemas.microsoft.com/winfx/2006/xaml` Namespace

As far as I can tell, at least in Avalonia, the things in this namespace are not backed by any real .NET symbols and are just made up by Rider. This is a problem because it only does this making up if it detects that you're using Avalonia.

The heuristic for "are we using Avalonia" appears to be finding an assembly or project reference for `Avalonia.Base`. You can see this in `XamlModulePlatformCache.cs` in the decompiled ReSharper source code and also the `XamlPlatform` flags enum.

For the time being I decided to hold off on actually fully pretending we're Avalonia so did *not* commit to a reference to `Avalonia.Base` yet. Instead we just manually created stuff like a dummy `StaticExtension` class (with a constructor that always throws) and put it into the `http://schemas.microsoft.com/winfx/2006/xaml` namespace. This seems to satisfy Rider without interfering with the actual XamlIL compilation.

## Markup Extensions

Markup extensions have to be classes with a method of signature either `object ProvideValue(IServiceProvider)` or `object ProvideValue()`.
It should be noted that Rider refuses to acknowledge markup extensions unless it finds Avalonia's `Avalonia.Data.Binding` type (or `Avalonia.Markup.Xaml.Data.Binding`). Yes, this is an actual requirement.
