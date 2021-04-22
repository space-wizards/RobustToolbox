# XamlUI notes about Rider

XAML support in both Rider and VS is pretty hardcoded for all supported frameworks. Rider seems less bad about this but they still hardcode a lot of type paths and do other silly stuff. Luckily, they support Avalonia. So... let's pretend to be Avalonia!

**JetBrains, if you're reading this, please don't sue me or take away our licenses pls I only reverse engineered your program to avoid wasting time on your issue tracker.**

## Where to find this info

The primary XAML support in Rider appears to be in `lib/ReSharperHost/JetBrains.ReSharper.Psi.Xaml.dll`. I recommend you decompile it with DotPeek to a project for navigating around. You can then look around the decompiled code (it's not obfuscated at all) to figure out how Rider does all this stuff.

## SDK Detection

Rider uses heuristics to determine which UI library you are using (WPF, Silverlight, Xamarin, Avalonia, ...). I would write "obviously, Rider needs to detect Avalonia to support Avalonia" but this actually didn't used to be the case until 2021.1 for half the features like XMLNS support.

However yes we live in a post-2021.1 society and Rider now needs to detect Avalonia to support Avalonia. The detection is that it looks for a project/assembly reference named `Avalonia.Base`. That's where that project in our tree comes from.

## Attributes

Attributes like `XmlnsAttributionAttribute` are hardcoded for full name with namespace. So we just define our own `Avalonia.Metadata.XmlnsDefinitionAttribute` and this is enough to satisfy Rider.

## `http://schemas.microsoft.com/winfx/2006/xaml` Namespace

This is the namespace that contains important markup extensions like `{x:Type}`. These types however are not backed by a real type at runtime, normally. Rider automatically "understands" them if it detects the Avalonia SDK.

It does appear that XamlIL still needs them to exist though, so we manually define things like `StaticExtension` in this namespace to appease XamlIL. Well...

I can't find equivalent types for these in the Avalonia repo, so it stands to reason XamlIL should have a better way to recognize them. The thing is that originally I had these types here to fool Rider without tricking it with SDK detection, but that's anymore (because the SDK detection is now 100% required). However when I removed the types XamlIL/GenerateTypeNameReferences broke, so... Yeah I can't be bothered to investigate further it seems to work currently.

## Markup Extensions

Markup extensions have to be classes with a method of signature either `object ProvideValue(IServiceProvider)` or `object ProvideValue()`.
It should be noted that Rider refuses to acknowledge markup extensions unless it finds Avalonia's `Avalonia.Data.Binding` type (or `Avalonia.Markup.Xaml.Data.Binding`). Yes, this is an actual requirement.
