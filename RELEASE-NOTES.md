# Release notes for RobustToolbox.

<!--
NOTE: automatically updated sometimes by version.py.
Don't change the format without looking at the script!
-->

<!--START TEMPLATE
## Master

### Breaking changes

*None yet*

### New features

*None yet*

### Bugfixes

*None yet*

### Other

*None yet*

### Internal

*None yet*


END TEMPLATE-->

## Master

### Breaking changes

*None yet*

### New features

* If a sandbox error is caused by a compiler-generated method, the engine will now attempt to point out which using code is responsible.
* Added `OrderedDictionary<TKey, TValue>` and `System.StringComparer` to the sandbox whitelist.
* Added more overloads to `MapLoaderSystem` taking `TextReader`/`TextWriter` where appropriate.

### Bugfixes

*None yet*

### Other

* Public APIs involving `System.Random` have been obsoleted. Use `IRobustRandom`/`RobustRandom` and such instead.

### Internal

*None yet*


## 272.0.0

### Breaking changes

* Reversed an undocumented breaking change from `v267.3.0`: entity spawning with a `MapCoordinates` now takes the rotation as relative to the map again instead of relative to the grid the entity was attached to.

### New features

* Added `ProfManager.Value` guard method.

### Bugfixes

* Fixed `ValidateMemberAnalyzer` taking a ridiculous amount of compile time.

### Other

* `ProfManager` is now initialized on the server.


## 271.2.0

### New features

* `IRobustSerializer` can now be configured to remove float NaN values when reading.
  * This is intended to blanket block cheat clients from sending NaN values in input commands they shouldn't.
  * To enable, set `IRobustSerializer.FloatFlags` from your content entrypoint.
  * If you do really want to send NaN values while using the above, you can use the new `UnsafeFloat`, `UnsafeHalf`, and `UnsafeDouble` types to indicate a field that is exempt.

### Other

* Improved some debug asserts related to contacts.

### Internal

* Warning cleanup.


## 271.1.0

### New features

* Added `AnimationStartedEvent` and `Control.AnimationStarted` events.

### Bugfixes

* Fixed the new transfer system not working.
* Fixed `NetManager` client state not getting reset properly when disconnected without call to `ClientDisconnect()`.

### Other

* The `launchauth` command now displays completions.


## 271.0.0

### Breaking changes

* Made types & methods related to `SharedNetworkResourceManager` internals `internal`.

### New features

* Added a new "high-bandwidth transfer" subsystem accessible via `ITransferManager`. Requires server-side setup with new CVars to get full benefit.
* Added `NetMessage.SequenceChannel`.
* Added `INetChannel.CanSendImmediately`.
* Added `[Animatable]` to some control properties.

### Bugfixes

* Localization string fixes that were causing warning spam.
* Fixed `MarkupNode.ToString()` not properly separating attributes with whitespace.
* Fixed `SpriteComponent.Layer` copy constructor not properly copying unshaded shaders.
* Fixed looped audio playback getting stuck in some cases by "exceeding" the length of the audio track.

### Other

* Resource uploads/downloads now use the new high-bandwidth transfer system.
* `DebugTools.AssertNotNull()` has been marked with `[NotNull]`, making C# nullable analysis recognize it.
* `SpriteView` updates its size when changing the set entity.
* Improved error message when directly referencing RT projects.
* Improve container remove assert message.
* `TabContainer` now wraps tabs if there are too many for the width of the control.
* The game now displays task bar progress when loading.
* Switched order of `TileChangedEvent` and `RegenerateCollision()` when setting tiles on grids, so the grid gets deleted *after* raising the event.
* Reverted change that made `SharedAudioSystem.Stop` not do nothing when the current tick has already been predicted.
* VV works with structs in components.
* Fixed `Robust.Benchmarks` failing to run due to project structure changes.
* Made component tree system more robust to errors to avoid entire game freezing.

### Internal

* Updated SDL3 to 3.4.0


## 270.1.0

### New features

* macOS: there is now tooling in place to build a content start binary to an app bundle in the development environment. This is a prerequisite for WebView support.
* Added override of `SharedPhysicsSystem.GetHardCollision` that takes a sole component.
* Added more parameters to `OutputPanel.AddMessage` & overloads.

### Other

* `ILocalizationManager.GetString` now logs a warning when failing to find a string. In cases where you expect this to happen use `TryGetString` instead.
* `run_server.bat` in published servers now `cd`s to the correct directory.
* `IRobustRandom.GetRandom()` is now obsolete. This API should've never existed.
* Started work on macOS support for WebView. This is not complete yet and will not work out of the box.
* Re-enable GPU compositing as the proper solution to the resizing bug has been found.
* `RichTextLabel.Text` sets all tags as allowed again, unlike `SetMessage()`.
* `EntProtoId<T>.TryGet` no longer throws if the prototype ID is invalid.


## 270.0.0

### Breaking changes

* Fixed `IClydeWindowInternal` erroneously being public.
* Added a new `[NotContentImplementable]` attribute and made many interfaces in the engine have it. This attribute marks that we may add members to these interfaces in the future, so content should not implement them.
* Removed unused `IRenderableComponent`, `IRand`, and `IPlayerInput` interfaces.

### New features

* Added `IsUiOpen` and `IsAnyUiOpen` to `SharedUserInterfaceSystem`. (was in previous engine release, missed in changelog)
* Added `game.time_scale` CVar.

### Bugfixes

* Fix a fake error being logged every time when setting the clipboard.
* Fixed audio loading by reverting dependency update to `VorbisPizza`.

### Other

* The size of the serializer string map is now logged.


## 269.0.1

### Bugfixes

* Fixed transitive project dependencies in content triggering "no direct project reference" detection.


## 269.0.0

### Breaking changes

* The project now targets .NET 10. You will have to install the new runtime on game servers when updating.
* We have adopted a new "solution management" system for games.
  * This enables us to add new projects to RT (e.g. split stuff up) without causing breaking changes.
  * Games must move to `.slnx` solutions and run `dotnet run --project ./RobustToolbox/Tools/Robust.SolutionGen/ -- update` after updating RT. This should be done after *every* RT feature update.
* Games may no longer directly reference RT projects. To depend on these, import the various `.props` files in the `Imports/` folder.
* We've tidied up all the transitive dependencies RT projects used to expose, meaning packages used by *Robust* aren't automatically visible to content projects anymore. You will likely have both accidental usages that are now erroring, or valid usages that you will need to add a `<PackageReference>` for.
* `OutputPanel` and `RichTextLabel` now set a default set of "safe" markup tags when using overloads that don't take in a `Type[]? allowedTags`. These tags are formatting only, so dangerous stuff like `[cmdlink]` is blocked by default.
* The constructor of `EntityQuery<TComp1>` has been made internal.

### New features

* Added `ExtensionMarkerAttribute`, used by the new C# 14 extension members, for the sandbox.
* Added `CommandWhenUIFocused` property to `Command` keybinds, to make them not fire when a UI control is focused.
* Startup logging now lists total memory and AVX10 intrinsics.
* Added new `FormattedString` type that represents a plain `string` that has markup formatting.
* Added an analyzer to detect redundant `[Prototype("foobar")]` strings.
* Added an analyzer to detect `DirtyField()` calls with incorrect field names.

### Bugfixes

* Fixed `FormattedMessage` not escaping plain text content properly with `.ToMarkup()`.
* Fixed wrapping on inline rich text controls like links.
* Fixed some native libs getting packaged for Linux clients when they shouldn't.
* Fixed `TilesEnumerator` being able to stack overflow due to the recursive implementation.
* Fixed some typos in `EntityDeserializer` log messages.
* Fixed WebView control resizing being fucky.
* Fixed `DataDefinitionAnalyzer` to recognize `[MeansDataDefinition]` attributes.

### Other

* Updated NuGet package dependencies.
* Prototype loading now tries to do some basic interning to avoid duplicate string objects being stored. This saves some memory.
* Avoid redundant texture uploads on WebView controls.
* Updated and added a lot of documentation to various parts of the engine.
* Moved to `.slnx`, and changed the default marker filename for hot reload to `.slnx` too.
* Removed GLFW windowing implementation.
* `EntityQuery.Resolve` now logs more info on error.
* Disabled some unnecessary .NET SDK source generators that slowed down build.
* Removed kdialog/nfd file dialog implementation.

### Internal

* Added a prototype `AspectRatioPanel` control. Not stabilized yet.
* Added gay colors to uitest.
* "Test content master" RT workflow now replaces `global.json` in SS14.
* Updated `Robust.LoaderApi` and `NetSerializer` to .NET 10.
* Fixed all the configurations in `RobustToolbox.sln`.
* Split up `Robust.UnitTesting` into many more projects.
* Internal warning fixes.


## 268.1.0

### New features

* Added `IReplayFileWriter.WriteYaml()`, for writing yaml documents to a replay zip file.
* Added Caps Lock as a proper bindable key.
* Added `IParallelBulkRobustJob` as an alternative to `IParallelRobustJob`, taking ranges instead of indices.
* Allow content to override `ProcessStream` and `GetOcclusion` in `AudioSystem`

### Bugfixes

* `ActorComponent` now has the `UnsavedComponentAttribute`
  * Previously it was unintentionally get serialized to yaml, which could result in NREs when deserializing.
* Don't spam error messages on startup trying to draw splash logos for projects that don't have one.
* Fix `SpriteSystem.LayerExists` saying that layer 0 is invalid.
* Fix `ButtonGroup`s unpressing buttons in an edge case with UI rebuilding.
* Added `CreatedTime` to `NetUserData`.
* Fix loading of `WebView`.

### Other

* Reverted undocumented change from 268.0.0 which obsoleted many `IoCManager` methods.
* Fix .NET 10 serializer compatibility of `BitArray`. (backported to older engines).
* Revert performance change to physics due to issues (double-buffered contact events).
* Audio entities are marked as `HideSpawnMenu` now.
* Make `SharedAudioSystem.Stop` not do nothing when the current tick has already been predicted.
* Warning cleanup.

### Internal

* Consolidated and updated physics benchmarks.


## 268.0.0

### Breaking changes

* Events that are raised via `IEventBus.RaiseComponentEvent()` now **must** be annotated with  the `ComponentEventAttribute`.
  * By default, events annotated with this attribute can **only** be raised via `IEventBus.RaiseComponentEvent()`. This can be configured via `ComponentEventAttribute.Exclusive`
* StartCollide and EndCollide events are now buffered until the end of physics substeps instead of being raised during the CollideContacts step. EndCollide events are double-buffered and any new ones raised while the events are being dispatched will now go out on the next tick / substep.

### New features

* Added `IUserInterfaceManager.ControlSawmill` and `Control.Log` properties so that controls can easily use logging without using static methods.


## 267.4.0

### New features

* Added two new custom yaml serializers `CustomListSerializer` and `CustomArraySerializer`.
* CVars defined in `[CVarDefs]` can now be private or internal.
* Added config rollback system to `IConfigurationManager`. This enables CVars to be snapshot and rolled back, even in the event of client crash.
* `OptionButton` now has a `Filterable` property that gives it a text box to filter options.
* Added `FontTagHijackHolder` to replace fonts resolved by `FontTag`.
* Sandbox:
  * Exposed `System.Reflection.Metadata.MetadataUpdateHandlerAttribute`.
  * Exposed more overloads on `StringBuilder`.
* The engine can now load system fonts.
  * At the moment only available on Windows.
  * See `ISystemFontManager` for API.
* The client now display a loading screen during startup.

### Bugfixes

* Fix `Menu` and `NumpadDecimal` key codes on SDL3.
* client-side predicted entity deletion ( `EntityManager.PredictedQueueDeleteEntity`) now behaves more like it does on the server. In particular, entities will be deleted on the same tick after all system have been updated. Previously, it would process deletions at the beginning of the next tick.
* Fix modifying `Label.FontOverride` not causing a layout update.
* Controls created by rich-text tags now get arranged to a proper size.
* Fix `OutputPanel` scrollbar breaking if a style update changes the font size.

### Other

* ComponentNameSerializer will now ignore any components that have been ignored via `IComponentFactory.RegisterIgnore`.
* Add pure to some SharedTransformSystem methods.
* Significantly optimised collision detection in SharedBroadphaseSystem.
* `Control.Stylesheet` does not do any work if assigning the value it already has.
* XAML hot reload now JITs UIs when first opened rather than doing every single one at client startup. This reduces dev startup overhead significantly and probably helps with memory usage too.

### Internal

* The `dmetamem` command now sorts its output, and doesn't output to log anymore to avoid output interleaving.


## 267.3.0

### New features

* Sandbox:
  * Added `System.DateOnly` and `System.TimeOnly`.
* `MapId`, `MapCoordinates`, and `EntityCoordinates` are now yaml serialisable
* The base component tree lookup system has new methods including several new `QueryAabb()` overloads that take in a collection and various new `IntersectRay()` overloads that should replace `IntersectRayWithPredicate`.
 * Added `OccluderSystem.InRangeUnoccluded()` for checking for occluders that lie between two points.
* `LocalizedCommands` now pass the command name as an argument to the localized help text.

### Bugfixes

* Fixed `MapLoaderSystem.SerializeEntitiesRecursive()` not properly serialising when given multiple root entities (e.g., multiple maps)
* Fixed yaml hot reloading throwing invalid path exceptions.
* The `EntityManager.CreateEntityUninitialized` overload that uses MapCoordinates now actually attaches entities to a grid if one is present at those coordinates, as was stated in it's documentation.
* Fixed physics joint relays not being properly updated when an entity is removed from a container.

### Other

* Updated natives again to attempt to fix issues caused by the previous update.


## 267.2.1


## 267.2.0

### New features

* Sprites and Sprite layers have a new `Loop` data field that can be set to false to automatically pause animations once they have finished.

### Bugfixes

* Fixed `CollectionExtensions.TryGetValue` throwing an exception when given a negative list index.
* Fixed `EntityManager.PredictedQueueDeleteEntity()` not deferring changes for networked entities until the end of the tick.
* Fixed `EntityManager.IsQueuedForDeletion` not returning true foe entities getting deleted via `PredictedQueueDeleteEntity()`

### Other

* `IResourceManager.GetContentRoots()` has been obsoleted and returns no more results.

### Internal

* `IResourceManager.GetContentRoots()` has been replaced with a similar method on `IResourceManagerInternal`. This new method returns `string`s instead of `ResPath`s, and usage code has been updated to use these paths correctly.


## 267.1.0

### New features

* Animation:
  * `AnimationTrackProperty.KeyFrame` can now have easings functions applied.
* Graphics:
  * `PointLightComponent` now has two fields, `falloff` and `curveFactor`, for controlling light falloff and the shape of the light attenuation curve.
  * `IClydeViewport` now has an `Id` and `ClearCachedResources` event. Together, these allow you to properly cache rendering resources per viewport.
* Miscellaneous:
  * Added `display.max_fps` CVar.
  * Added `IGameTiming.FrameStartTime`.
* Sandbox:
  * Added `System.WeakReference<T>`.
  * Added `SpaceWizards.Sodium.CryptoGenericHashBlake2B.Hash()`.
  * Added `System.Globalization.UnicodeCategory`.
* Serialization:
  * Added a new entity yaml deserialization option (`SerializationOptions.EntityExceptionBehaviour`) that can optionally make deserialization more exception tolerant.
* Tooling:
  * `devwindow` now has a tab listing active `IRenderTarget`s, allowing insight into resource consumption.
  * `loadgrid` now creates a map if passed an invalid map ID.
  * Added game version information to F3 overlay.
  * Added completions to more map commands.
* UI system:
  * `Control.OrderedChildCollection` (gotten from `.Children`) now implements `IReadOnlyList<Control>`, allowing it to be indexed directly.
    * Added `WrapContainer` control. This lays out multiple elements along an axis, wrapping them if there's not enough space. It comes with many options and can handle multiple axes.
  * Popups/modals now work in secondary windows. This entails putting roots for these on each UI root.
  * If you are not using `OSWindow` and are instead creating secondary windows manually, you need to call `WindowRoot.CreateRootControls()` manually for this to work.
  * Added `Axis` enum, `IAxisImplementation` interface and axis implementations. These allow writing general-purpose UI layout code that can work on multiple axis at once.
* WebView:
  * Added `web.remote_debug_port` CVar to change Chromium's remote debug port.

### Bugfixes

* Audio:
  * Fix audio occlusion & velocity being calculated with the audio entity instead of the source entity.
* Bound UI:
  * Try to fix an assert related to `UserInterfaceComponent` delta states.
* Configuration:
  * The client no longer tries to send `CLIENT | REPLICATED` CVars when not connected to a server. This could cause test failures.
* Math:
  * Fixed `Matrix3Helpers.TransformBounds()` returning an incorrect result. Now it effectively behaves like `Matrix3Helpers.TransformBox()` and has been marked as obsolete.
* Physics:
  * Work around an undiagnosed crash processing entities without parents.
* Serialization:
  * Fix `[DataRecord]`s with computed get-only properties.
* Resources:
  * Fix some edge case broken path joining in `DirLoader` and `WritableDirProvider`.
* Tests:
  * Fix `PlacementManager.CurrentMousePosition` in integration tests.
* UI system:
  * Animations for the debug console and scrolling are no longer framerate dependent.
  * Fix `OutputPanel.SetMessage` triggering a scrolling animation when editing messages other than the last one.
  * Fix word wrapping with two-`char` runes in `RichTextLabel` and `OutputPanel`.
* WebView:
  * Multiple clients with WebView can now run at the same time, thanks to better CEF cache management.

### Other

* Audio:
  * Improved error logging for invalid file names in `SharedAudioSystem`.
* Configuration:
  * Fix crash if more than 255 `REPLICATED` CVars exist. Also increased the max size of the CVar replication message.
* Entities:
  * Transform:
    * `AnchorEntity` logs instead of using an assert for invalid arguments.
  * Containers:
    * `SharedContainerSystem.CleanContainer` now uses `PredictedDel()` instead.
* Networking:
  * The client now logs an error when attempting to send a network message without server connection. Previously, it would be silently dropped.
  * `net.interp` and `net.buffer_size` CVars are now `REPLICATED`.
* Graphics:
  * The function used for pointlight attenuation has been modified to be c1 continuous as opposed to simply c0 continuous, resulting in smoother boundary behavior.
  * RSI validator no longer allows empty (`""`) state names.
* Packaging:
  * Server packaging now excludes all files in the `Audio/` directory.
  * Server packaging now excludes engine resources `EngineFonts/` and `Midi/`.
  * ACZ explicitly specifies manifest charset as UTF-8.
* Serialization:
  * `CurTime`-relative `TimeSpan` values that are `MaxValue` now deserialize without overflow.
  * `SpriteSpecifier.Texture` will now fail to validate if the path is inside a `.rsi`. Use RSI sprite specifiers instead.
* Resources:
  * `IWritableDirProvider.RootDir` is now null on clients.
* WebView:
  * CEF cache is no longer in the content-accessible user data directory.

### Internal

* Added some debug commands for debugging viewport resource management: `vp_clear_all_cached` & `vp_test_finalize`
* `uitest` command now supports command argument for tab selection, like `uitest2`.
* Rewrote `BoxContainer` implementation to make use of new axis system.
* Moved `uitest2` and `devwindow` to use the `OSWindow` control.
* SDL3 binding has been moved to `SpaceWizards.Sdl` NuGet package.
* `dmetamem` command has been moved from `DEBUG` to `TOOLS`.
* Consolidate `AttachToGridOrMap` with `TryGetMapOrGridCoordinates`.
* Secondary window render targets have clear names specified.
* Updated `SpaceWizards.NFluidsynth` to `0.2.2`.
* `Robust.Client.WebView.Cef.Program` is now internal.
* `download_manifest_file.py` script in repo now always decodes as UTF-8 correctly.
* Added a new debug assert to game state processing.

## 267.0.0

### Breaking changes

* When a player disconnects, the relevant callbacks are now fired *after* removing the channel from `INetManager`.

### New features

* Engine builds are now published for ARM64 & FreeBSD.
* CPU model names are now detected on Windows & Linux ARM64.
* Toolshed's `spawn:in` command now works on entities without `Physics` component.

### Bugfixes

* SDL3 windowing backend fixes:
  * Avoid macOS freezes with multiple windows.
  * Fix macOS rendering breaking when closing secondary windows.
  * File dialogs properly associate parent windows.
  * Fix IME positions not working with UI scaling properly.
  * Properly specify library names for loading native library.

* WinBit threads don't permanently stay stuck when their window closes.
* Checking for the "`null`" literal in serialization is now culture invariant.

### Other

* Compat mode on the client now defaults to on for Windows Snapdragon devices, to work around driver bugs.
* Update various libraries & natives. This enables out-of-the-box ARM64 support on all platforms and is a long-overdue modernization.
* Key name displays now use proper Unicode symbols for macOS ⌥ and ⌘.
* Automated CI for RobustToolbox runs on macOS again.
* Autocompletions for `ProtoId<T>` in Toolshed now use `PrototypeIdsLimited` instead of arbitrarily cutting out if more than 256 of a prototype exists.


## 266.0.0

### Breaking changes

* A new analyzer has been added that will error if you attempt to subscribe to `AfterAutoHandleStateEvent` on a
  component that doesn't have the `AutoGenerateComponentState` attribute, or doesn't have the first argument of that
  attribute set to `true`. In most cases you will want to set said argument to `true`.
* The fields on `AutoGenerateComponentStateAttribute` are now `readonly`. Setting these directly (instead of using the constructor arguments) never worked in the first place, so this change only catches existing programming errors.
* When a player disconnects, `ISharedPlayerManager.PlayerStatusChanged` is now fired *after* removing the session from the `Sessions` list.
* `.rsi` files are now compacted into individual `.rsic` files on packaging. This should significantly reduce file count & improve performance all over release builds, but breaks the ability to access `.png` files into RSIs directly. To avoid this, `"rsic": false` can be specified in the RSI's JSON metadata.
* The `scale` command has been removed, with the intent of it being moved to content instead.

### New features

* ViewVariables editors for `ProtoId` fields now have a Select button which opens a window listing all available prototypes of the appropriate type.
* added **IConfigurationManager**.*SubscribeMultiple* ext. method to provide simpler way to unsubscribe from multiple cvar at once
* Added `SharedMapSystem.QueueDeleteMap`, which deletes a map with the specified MapId in the next tick.
* Added generic version of `ComponentRegistry.TryGetComponent`.
* `AttributeHelper.HasAttribute` has had an overload's type signature loosened from `INamedTypeSymbol` to `ITypeSymbol`.
* Errors are now logged when sending messages to disconnected `INetChannel`s.
* Warnings are now logged if sending a message via Lidgren failed for some reason.
* `.yml` and `.ftl` files in the same directory are now concatenated onto each other, to reduce file count in packaged builds. This is done through the new `AssetPassMergeTextDirectories` pass.
* Added `System.Linq.ImmutableArrayExtensions` to sandbox.
* `ImmutableDictionary<TKey, TValue>` and `ImmutableHashSet<T>` can now be network serialized.
* `[AutoPausedField]` now works on fields of type `Dictionary<TKey, TimeSpan>`.
* `[NotYamlSerializable]` analyzer now detects nullable fields of the not-serializable type.
* `ItemList` items can now have a scale applied for the icon.
* Added new OS mouse cursor shapes for the SDL3 backend. These are not available on the GLFW backend.
* Added `IMidiRenderer.MinVolume` to scale the volume of MIDI notes.
* Added `SharedPhysicsSystem.ScaleFixtures`, to apply the physics-only changes of the prior `scale` command.

### Bugfixes

* `LayoutContainer.SetMarginsPreset` and `SetAnchorAndMarginPreset` now correctly use the provided control's top anchor when calculating the margins for its presets; it previously used the bottom anchor instead. This may result in a few UI differences, by a few pixels at most.
* `IConfigurationManager` no longer logs a warning when saving configuration in an integration test.
* Fixed impossible-to-source `ChannelClosedException`s when sending some net messages to disconnected `INetChannel`s.
* Fixed an edge case causing some color values to throw an error in `ColorNaming`.
* Fresh builds from specific projects should no longer cause errors related to `Robust.Client.Injectors` not being found.
* Stopped errors getting logged about `NoteOff` and `NoteOn` operations failing in MIDI.
* Fixed MIDI players not resuming properly when re-entering PVS range.

### Other

* Updated ImageSharp to 3.1.11 to stop the warning about a DoS vulnerability.
* Prototype YAML documents that are completely empty are now skipped by the prototype loader. Previously they would cause a load error for the whole file.
* `TileSpawnWindow` can now be localized.
* `BaseWindow` uses the new mouse cursor shapes for diagonal resizing.
* `NFluidsynth` has been updated to 0.2.0

### Internal

* Added `uitest` tab for standard mouse cursor shapes.


## 265.0.0

### Breaking changes

* More members in `IntegrationInstance` now enforce that the instance is idle before accessing it.
* `Prototype.ValidateDirectory` now requires that prototype IDs have no spaces or periods in them.
* `IPrototypeManager.TryIndex` no longer logs errors unless using the overload with an optional parameter. Use `Resolve()` instead if error logging is desired.
* `LocalizedCommands` now has a `Loc` property that refers to `LocalizationManager`. This can cause compile failures if you have static methods in child types that referenced static `Loc`.
* `[AutoGenerateComponentState]` now works on parent members for inherited classes. This can cause compile failures in certain formerly silently broken cases with overriden properties.
* `Vector3`, `Vector4`, `Quaternion`, and `Matrix4` have been removed from `Robust.Shared.Maths`. Use the `System.Numerics` types instead.

### New features

* `RobustClientPackaging.WriteClientResources()` and `RobustServerPackaging.WriteServerResources()` now have an overload taking in a set of things to ignore in the content resources directory.
* Added `IPrototypeManager.Resolve()`, which logs an error if the resolved prototype does not exist. This is effectively the previous (but not original) default behavior of `IPrototypeManager.TryIndex`.
* There's now a ViewVariables property editor for tuples.
* Added `ColorNaming` helper functions for getting textual descriptions of color values.
* Added Oklab/Oklch conversion functions for `Color`.
* `ColorSelectorSliders` now displays textual descriptions of color values.
* Added `TimeSpanExt.TryTimeSpan` to parse `TimeSpan`s with the `1.5h` format available in YAML.
* Added `ITestContextLike` and related classes to allow controlling pooled integration instances better.
* `EntProtoId` VV prop editors now don't allow setting invalid prototype IDs, inline with `ProtoId<T>`.
* Custom VV controls can now be registered using `IViewVariableControlFactory`.
* The entity spawn window now shows all placement modes registered with `IPlacementManager`.
* Added `VectorHelpers.InterpolateCubic` for `System.Numerics` `Vector3` and `Vector4`.
* Added deconstruct helpers for `System.Numerics` `Vector3` and `Vector4`.

### Bugfixes

* Pooled integration instances returned by `RobustIntegrationTest` are now treated as non-idle, for consistency with non-pooled startups.
* `SharedAudioSystem.SetState` no longer calls `DirtyField` on `PlaybackPosition`, an unnetworked field.
* Fix loading texture files from the root directory.
* Fix integration test pooling leaking non-reusable instances.
* Fix multiple bugs where VV displayed the wrong property editor for remote values.
* VV displays group headings again in member list.
* Fix a stack overflow that could occur with `ColorSelectorSliders`.
* `MidiRenderer` now properly handles `NoteOn` events with 0 velocity (which should actually be treated as `NoteOff` events).

### Other

* The debug assert for `RobustRandom.Next(TimeSpan, TimeSpan)` now allows for the two arguments to be equal.
* The configuration system will now report an error instead of warning if it fails to load the config file.
* Members in `IntegrationInstance` that enforce the instance is idle now always allow access from the instance's thread (e.g. from a callback).
* `IPrototypeManager` methods now have `[ForbidLiteral]` where appropriate.
* Performance improvements to physics system.
* `[ValidatePrototypeIdAttribute]` has been marked as obsolete.
* `ParallelManager` no longer cuts out exception information for caught job exceptions.
* Improved logging for PVS uninitialized/deleted entity errors.

### Internal

* General code & warning cleanup.
* Fix `VisibilityTest` being unreliable.
* `ColorSelectorSliders` has been internally refactored.
* Added CI workflows that test all RT build configurations.

## 264.0.0

### Breaking changes

* `IPrototypeManager.Index(Type kind, string id)` now throws `UnknownPrototypeException` instead of `KeyNotFoundException`, for consistency with `IPrototypeManager.Index<T>`.

### New features

* Types can now implement the new interface `IRobustCloneable<T>` to be cloned by the component state source generator.
* Added extra Roslyn Analyzers to detect some misuse of prototypes:
  * Network serializing prototypes (tagging them with `[Serializable, NetSerializable]`).
  * Constructing new instances of prototypes directly.
* Add `PrototypeManagerExt.Index` helper function that takes a nullable `ProtoId<T>`, returning null if the ID is null.
* Added an `AlwaysActive` field to `WebViewControl` to make a browser window active even when not in the UI tree.
* Made some common dependencies accessible through `IPlacementManager`.
* Added a new `GENITIVE()` localization helper function, which is useful for certain languages.

### Bugfixes

* Sprite scale is now correctly applied to sprite boundaries in `SpriteSystem.GetLocalBounds`.
* Fixed documentation for `IPrototypeManager.Index<T>` stating that `KeyNotFoundException` gets thrown, when in actuality `UnknownPrototypeException` gets thrown.

### Other

* More tiny optimizations to `DataDefinitionAnalyzer`.
* NetSerializer has been updated. On debug, it will now report *where* a type that can't be serialized is referenced from.

### Internal

* Minor internal code cleanup.


## 263.0.0

### Breaking changes

* Fully removed some non-`Entity<T>` container methods.

### New features

* `IMidiRenderer.LoadSoundfont` has been split into `LoadSoundfontResource` and `LoadSoundfontUser`, the original now being deprecated.
* Client command execution now properly catches errors instead of letting them bubble up through the input stack.
* Added `CompletionHelper.PrototypeIdsLimited` API to allow commands to autocomplete entity prototype IDs.
* Added `spawn:in` Toolshed command.
* Added `MapLoaderSystem.TryLoadGeneric` overload to load from a `Stream`.
* Added `OutputPanel.GetMessage()` and `OutputPanel.SetMessage()` to allow replacing individual messages.

### Bugfixes

* Fixed debug asserts when using MIDI on Windows.
* Fixed an error getting logged on startup on macOS related to window icons.
* `CC-BY-NC-ND-4.0` is now a valid license for the RGA validator.
* Fixed `TabContainer.CurrentTab` clamping against the wrong value.
* Fix culture-based parsing in `TimespanSerializer`.
* Fixed grid rendering blowing up on tile IDs that aren't registered.
* Fixed debug assert when loading MIDI soundfonts on Windows.
* Make `ColorSelectorSliders` properly update the dropdown when changing `SelectorType`.
* Fixed `tpto` allowing teleports to oneself, thereby causing them to be deleted.
* Fix OpenAL extensions being requested incorrectly, causing an error on macOS.
* Fixed horizontal measuring of markup controls in rich text.

### Other

* Improved logging for some audio entity errors.
* Avoided more server stutters when using `csci`.
* Improved physics performance.
* Made various localization functions like `GENDER()` not throw if passed a string instead of an `EntityUid`.
* The generic clause on `EntitySystem.AddComp<T>` has been changed to `IComponent` (from `Component`) for consistency with `IEntityManager.AddComponent<T>`.
* `DataDefinitionAnalyzer` has been optimized somewhat.
* Improved assert logging error message when static data fields are encountered.

### Internal

* Warning cleanup.
* Added more tests for `DataDefinitionAnalyzer`.
* Consistently use `EntitySystem` proxy methods in engine.


## 262.0.0

### Breaking changes

* Toolshed commands will now validate that each non-generic command argument is parseable (i.e., has a corresponding type parser). This check can be disabled by explicitly marking the argument as unparseable via `CommandArgumentAttribute.Unparseable`.

### New features

* `ToolshedManager.TryParse` now also supports nullable value types.
* Add an ignoredComponents arg to IsDefault.

### Bugfixes

* Fix `SpriteComponent.Layer.Visible` setter not marking a sprite's bounding box as dirty.
* The audio params in the passed SoundSpecifier for PlayStatic(SoundSpecifier, Filter, ...) will now be used as a default like other PlayStatic overrides.
* Fix windows not saving their positions correctly when their x position is <= 0.
* Fix transform state handling overriding PVS detachment.


## 261.2.0

### New features

* Implement IEquatable for ResolvedPathSpecifier & ResolvedCollectionSpecifier.
* Add NearestChunkEnumerator.

### Bugfixes

* Fix static entities not having the center of mass updated.
* Fix TryQueueDelete.
* Fix tpto potentially parenting grids to non-map entities.

### Other

* TileChangedEvent is now raised once in clientside grid state handling rather than per tile.
* Removed ITileDefinition.ID as it was redundant.
* Change the lifestage checks on predicted entity deletion to check for terminating.

### Internal

* Update some `GetComponentName<T>` uses to generic.


## 261.1.0

### New features

* Automatically create logger sawmills for `UIController`s similar to `EntitySystem`s.

### Bugfixes

* Fix physics forces not auto-clearing / respecting the cvar.

### Internal

* Cleanup more compiler warnings in unit tests.


## 261.0.0

### Breaking changes

* Remove unused TryGetContainingContainer override.
* Stop recursive FrameUpdates for controls that are not visible.
* Initialize LocMgr earlier in the callstack for GameController.
* Fix FastNoiseLise fractal bounding and remove its DataField property as it should be derived on other properties updating.
* Make RaiseMoveEvent internal.
* MovedGridsComponent and PhysicsMapComponent are now purged and properties on `SharedPhysicsSystem`. Additionally the TransformComponent for Awake entities is stored alongside the PhysicsComponent for them.
* TransformComponent is now stored on physics contacts.
* Gravity2DComponent and Gravity2DController were moved to SharedPhysicsSystem.

### New features

* `IFileDialogManager` now allows specifying `FileAccess` and `FileShare` modes.
* Add Intersects and Enlarged to Box2i in line with Box2.
* Make `KeyFrame`s on `AnimationTrackProperty` public settable.
* Add the spawned entities to a returned array from `SpawnEntitiesAttachedTo`.

### Bugfixes

* Fixed SDL3 file dialog implementation having a memory leak and not opening files read-write.
* Fix GetMapLinearVelocity.

### Other

* `uploadfile` and `loadprototype` commands now only open files with read access.
* Optimize `ToMapCoordinates`.

### Internal

* Cleanup on internals of `IFileDialogManager`, removing duplicate code.
* Fix Contacts not correctly being marked as `Touching` while contact is ongoing.


## 260.2.0

### New features

* Add `StringBuilder.Insert(int, string)` to sandbox.
* Add the WorldNormal to the StartCollideEvent.


## 260.1.0

### New features

* `ComponentFactory` is now exposed to `EntitySystem` as `Factory`

### Other

* Cleanup warnings in PLacementManager
* Cleanup warnings in Clide.Sprite

## 260.0.0

### Breaking changes

* Fix / change `StartCollideEvent.WorldPoint` to return all points for the collision which may be up to 2 instead of 1.

### New features

* Add SpriteSystem dependency to VisualizerSystem.
* Add Vertical property to progress bars
* Add some `EntProtoId` overloads for group entity spawn methods.


## 259.0.0

### Breaking changes

* TileChangedEvent now has an array of tile changed entries rather than raising an individual event for every single tile changed.

### Other

* `Entity<T>` methods were marked as `readonly` as appropriate.


## 258.0.1

### Bugfixes

* Fix static physics bodies not generating contacts if they spawn onto sleeping bodies.


## 258.0.0

### Breaking changes

* `IMarkupTag` and related methods in `MarkupTagManager` have been obsoleted and should be replaced with the new `IMarkupTagHandler` interface. Various engine tags (e.g., `BoldTag`, `ColorTag`, etc) no longer implement the old interface.

### New features

* Add IsValidPath to ResPath and make some minor performance improvements.

### Bugfixes

* OutputPanel and RichTextLabel now remove controls associated with rich text tags when the text is updated.
* Fix `SpriteComponent.Visible` datafield not being read from yaml.
* Fix container state handling not forcing inserts.

### Other

* `SpriteSystem.LayerMapReserve()` no longer throws an exception if the specified layer already exists. This makes it behave like the obsoleted `SpriteComponent.LayerMapReserveBlank()`.


## 257.0.2

### Bugfixes

* Fix unshaded sprite layers not rendering correctly.


## 257.0.1

### Bugfixes

* Fix sprite layer bounding box calculations. This was causing various sprite rendering & render-tree lookup issues.


## 257.0.0

### Breaking changes

* The client will now automatically pause any entities that leave their PVS range.
* Contacts for terminating entities no longer raise wake events.

### New features

* Added `IPrototypeManager.IsIgnored()` for checking whether a given prototype kind has been marked as ignored via `RegisterIgnore()`.
* Added `PoolManager` & `TestPair` classes to `Robust.UnitTesting`. These classes make it easier to create & use pooled server/client instance pairs in integration tests.
* Catch NotYamlSerializable DataFields with an analyzer.
* Optimized RSI preloading and texture atlas creation.

### Bugfixes

* Fix clients unintentionally un-pausing paused entities that re-enter pvs range

### Other

* The yaml prototype id serialiser now provides better feedback when trying to validate an id for a prototype kind that has been ignored via `IPrototypeManager.RegisterIgnore()`
* Several SpriteComponent methods have been marked as obsolete, and should be replaced with new methods in SpriteSystem.
* Rotation events no longer check for grid traversal.


## 256.0.0

### Breaking changes

* `ITypeReaderWriter<TType, TNode>` has been removed due to being unused. Implement `ITypeSerializer<TType, TNode>` instead
* Moved AsNullable extension methods to the Entity struct.

### New features

* Add DevWindow tab to show all loaded textures.
* Add Vector2i / bitmask converfsion helpers.
* Allow texture preload to be skipped for some textures.
* Check audio file signatures instead of extensions.
* Add CancellationTokenRegistration to sandbox.
* Add the ability to serialize TimeSpan from text.
* Add support for rotated / mirrored tiles.

### Bugfixes

* Fix yaml hot reloading.
* Fix a linear dictionary lookup in PlacementManager.

### Other

* Make ItemList not run deselection callback on all items if they aren't selected.
* Cleanup warnings for CS0649 & CS0414.

### Internal

* Move PointLight component states to shared.


## 255.1.0

### New features

* The client localisation manager now supports hot-reloading ftl files.
* TransformSystem can now raise `GridUidChangedEvent` and `MapUidChangedEvent` when a entity's grid or map changes. This event is only raised if the `ExtraTransformEvents` metadata flag is enabled.

### Bugfixes

* Fixed a server crash due to a `NullReferenceException` in PVS system when a player's local entity is also one of their view subscriptions.
* Fix CompileRobustXamlTask for benchmarks.
* .ftl files will now hot reload.
* Fix placementmanager sometimes not clearing.

### Other

* Container events are now documented.


## 255.0.0

### Breaking changes

* `RobustIntegrationTest` now pools server/client instances by default. If a custom settings class is provided, it will still disable pooling unless explicitly enabled.
  * Server/Client instances that are returned to the pool should be disconnected. This might require you to update some tests.
  * Pooled instances also require you to use `RobustIntegrationTest` methods like `WaitPost()` to ensure the correct thread is used.

### Bugfixes

* Fix `EntityDeserializer` improperly setting entity lifestages when loading a post-mapinit map.
* Fix `EntityManager.PredictedDeleteEntity()` not deleting pure client-side entities.
* Fix grid fixtures using a locale dependent id. This could cause some clients to crash/freeze when connected to a server with a different locale.

### Other

* Add logic to block cycles in master MIDI renderers, which could otherwise cause client freezes.


## 254.1.0

### New features

* Add CC ND licences to the RGA validator.
* Add entity spawn prediction and entity deletion prediction. This is currently limited as you are unable to predict interactions with these entities. These are done via the new methods prefixed with "Predicted". You can also manually flag an entity as a predicted spawn with the `FlagPredicted` method which will clean it up when prediction is reset.

### Bugfixes

* Fix tile edge rendering for neighbor tiles being the same priority.

### Other

* Fix SpawnAttachedTo's system proxy method not the rotation arg like EntityManager.


## 254.0.0

### Breaking changes

* Yaml mappings/dictionaries now only support string keys instead of generic nodes
  * Several MappingDataNode method arguments or return values now use strings instead of a DataNode object
  * The MappingDataNode class has various helper methods that still accept a ValueDataNode, but these methods are marked as obsolete and may be removed in the future.
  * yaml validators should use `MappingDataNode.GetKeyNode()` when validating mapping keys, so that errors can print node start & end information
* ValueTuple yaml serialization has changed
  * Previously they would get serialized into a single mapping with one entry (i.e., `{foo : bar }`)
  * Now they serialize into a sequence (i.e., `[foo, bar]`)
  * The ValueTuple serializer will still try to read mappings, but due to the MappingDataNode this may fail if the previously serialized "key" can't be read as a simple string

### New features

* Add cvar to disable tile edges.
* Add GetContainingContainers method to ContainerSystem to recursively get containers upwards on an entity.

### Internal

* Make component lifecycle methods use generics.


## 253.0.0

### New features

* Add a new `SerializationManager.PushComposition()` overload that takes in a single parent instead of an array of parents.
* `BoundUserInterfaceMessageAttempt` once again gets raised as a broadcast event, in addition to being directed.
  * This effectively reverts the breaking part of the changes made in v252.0.0
* Fix CreateDistanceJoint using an int instead of a float for minimum distance.

### Bugfixes

* Fix deferred component removal not setting the component's life stage to `ComponentLifeStage.Stopped` if the component has not yet been initialised.
* Fix some `EntitySystem.Resolve()` overloads not respecting the optional `logMissing` argument.
* Fix screen-space overlays not being useable without first initializing/starting entity manager & systems
* ItemList is now significantly optimized. VV's `AddComponent` window in particular should be much faster.
* Fix some more MapValidator fields.
* Fix popup text overflowing the sides of the screen.
* Improve location reporting for non-writeable datafields via analyzer.

### Other

* TestPoint now uses generics rather than IPhysShape directly.


## 252.0.0

### Breaking changes

* BoundUserInterfaceMessageAttempt is raised directed against entities and no longer broadcast.


## 251.0.0

### Breaking changes

* Localization is now separate between client and server and is handled via cvar.
* Contacting entities no longer can be disabled for CollisionWake to avoid destroying the contacts unnecessarily.

### New features

* Added `DirectionExtensions.AllDirections`, which contains a list of all `Direction`s for easy enumeration.
* Add ForbidLiteralAttribute.
* Log late MsgEntity again.
* Show entity name in `physics shapeinfo` output.
* Make SubscribeLocalEvent not require EntityEventArgs.
* Add autocomplete to `tp` command.
* Add button to jump to live chat when scrolled up.
* Add autocomplete to `savemap` and `savegrid`.

### Bugfixes

* Fix velocity not re-applying correctly on re-parenting.
* Fix Equatable on FormattedMessage.
* Fix SharedTransformSystem methods logging errors on resolves.

### Other

* Significantly optimized tile edge rendering.

### Internal

* Remove duplicate GetMassData method.
* Inline manifold points for physics.


## 250.0.0

### Breaking changes

* The default shader now interprets negative color modulation as a flag that indicates that the light map should be ignored.
  * This can be used to avoid having to change the light map texture, thus reducing draw batches.
  * Sprite layers that are set to use the "unshaded" shader prototype now use this.
  * Any fragment shaders that previously the `VtxModulate` colour modulation variable should instead use the new `MODULATE` variable, as the former may now contain negative values.

### New features

* Add OtherBody API to contacts.
* Make FormattedMessages Equatable.
* AnimationCompletionEvent now has the AnimationPlayerComponent.
* Add entity description as a tooltip on the entity spawn panel.

### Bugfixes

* Fix serialization source generator breaking if a class has two partial locations.
* Fix map saving throwing a `DirectoryNotFoundException` when given a path with a non-existent directory. Now it once again creates any missing directories.
* Fix map loading taking a significant time due to MappingDataNode.Equals calls being slow.

### Other

* Add Pure to some Angle methods.

### Internal

* Cleanup some warnings in classes.


## 249.0.0

### Breaking changes

* Layer is now read-only on VisibilityComponent and isn't serialized.

### New features

* Added a debug overlay for the linear and angular velocity of all entities on the screen. Use the `showvel` and `showangvel` commands to toggle it.
* Add a GetWorldManifold overload that doesn't require a span of points.
* Added a GetVisMaskEvent. Calling `RefreshVisibilityMask` will raise it and subscribers can update the vismask via the event rather than subscribers having to each manually try and handle the vismask directly.

### Bugfixes

* `BoxContainer` no longer causes stretching children to go below their minimum size.
* Fix lights on other grids getting clipped due to ignoring the light range cvar.
* Fix the `showvelocities` command.
* Fix the DirtyFields overload not being sandbox safe for content.

### Internal

* Polygon vertices are now inlined with FixedArray8 and a separate SlimPolygon using FixedArray4 for hot paths rather than using pooled arrays.


## 248.0.2

### Bugfixes

* Don't throw in overlay rendering if MapUid not found.

### Internal

* Reduce EntityManager.IsDefault allocations.


## 248.0.1

### Bugfixes

* Bump ImageSharp version.
* Fix instances of NaN gain for audio where a negative-infinity value is being used for volume.


## 248.0.0

### Breaking changes

* Use `Entity<MapGridComponent>` for TileChangedEvent instead of EntityUid.
* Audio files are no longer tempo perfect when being played if the offset is small. At some point in the future an AudioParams bool is likely to be added to enforce this.
* MoveProxy method args got changed in the B2DynamicTree update.
* ResPath will now assert in debug if you pass in an invalid path containing the non-standardized directory separator.

### New features

* Added a new `MapLoaderSystem.TryLoadGrid()` override that loads a grid onto a newly created map.
* Added a CVar for the endbuffer for audio. If an audio file will play below this length (for PVS reasons) it will be ignored.
* Added Regex.Count + StringBuilder.Chars setter to the sandbox.
* Added a public API for PhysicsHull.
* Made MapLoader log more helpful.
* Add TryLoadGrid override that also creates a map at the same time.
* Updated B2Dynamictree to the latest Box2D V3 version.
* Added SetItems to ItemList control to set items without removing the existing ones.
* Shaders, textures, and audio will now hot reload automatically to varying degrees. Also added IReloadManager to handle watching for file-system changes and relaying events.
* Wrap BUI disposes in a try-catch in case of exceptions.


### Bugfixes

* Fix some instances of invalid PlaybackPositions being set.
* Play audio from the start of a file if it's only just come into PVS range / had its state handled.
* Fix TryCopyComponents.
* Use shell.WriteError if TryLoad fails for mapping commands.
* Fix UI control position saving causing exceptions where the entity is cleaned-up alongside a state change.
* Fix Map NetId completions.
* Fix some ResPath calls using the wrong paths.

### Internal

* Remove some unused local variables and the associated warnings.


## 247.2.0

### New features

* Added functions for copying components to `IEntityManager` and `EntitySystem`.
* Sound played from sound collections is now sent as "collection ID + index" over the network instead of the final filename.
  * This enables integration of future accessibility systems.
  * Added a new `ResolvedSoundSpecifier` to represent played sounds. Methods that previously took a filename now take a `ResolvedSoundSpecifier`, with an implicit cast from string being interpreted as a raw filename.
* `VisibilitySystem` has been made accessible to shared as `SharedVisibilitySystem`.
* `ScrollContainer` now has properties exposing `Value` and `ValueTarget` on its internal scroll bars.

### Bugfixes

* Fix prototype hot reload crashing when adding a new component already exists on an entity.
* Fix maps failing to save in some cases related to tilemap IDs.
* Fix `Regex.Escape(string)` not being available in sandbox.
* Prototypes that parent themselves directly won't cause the game to hang on an infinite loop anymore.
* Fixed disconnecting during a connection attempt leaving the client stuck in a phantom state.

### Internal

* More warning cleanup.

## 247.1.0

### New features

* Added support for `Color[]` shader uniforms
* Added optional minimumDistance parameter to `SharedJointSystem.CreateDistanceJoint()`

### Bugfixes

* Fixed `EntitySystem.DirtyFields()` not actually marking fields as dirty.

### Other

* Updated the Yamale map file format validator to support v7 map/grid files.


## 247.0.0

### Breaking changes

* `ITileDefinitionManager.AssignAlias` and general tile alias functionality has been removed. `TileAliasPrototype` still exist, but are only used during entity deserialization.
* `IMapManager.AddUninitializedMap` has been removed. Use the map-init options on `CreateMap()` instead.
* Re-using a MapId will now log a warning. This may cause some integration tests to fail if they are configured to fail
  when warnings are logged.
* The minimum supported map format / version has been increased from 2 to 3.
* The server-side `MapLoaderSystem` and associated classes & structs has been moved to `Robust.Shared`, and has been significantly modified.
  * The `TryLoad` and `Save` methods have been replaced with grid, map, generic entity variants. I.e, `SaveGrid`, `SaveMap`, and `SaveEntities`.
  * Most of the serialization logic and methods have been moved out of `MapLoaderSystem` and into new `EntitySerializer`
    and `EntityDeserializer` classes, which also replace the old `MapSerializationContext`.
  * The `MapLoadOptions` class has been split into `MapLoadOptions`, `SerializationOptions`, and `DeserializationOptions`
    structs.
* The interaction between PVS overrides and visibility masks / layers have changed:
  * Any forced entities (i.e., `PvsOverrideSystem.AddForceSend()`) now ignore visibility masks.
  * Any global & session overrides (`PvsOverrideSystem.AddGlobalOverride()` & `PvsOverrideSystem.AddSessionOverride()`) now respect visibility masks.
  * Entities added via the `ExpandPvsEvent` respect visibility masks.
  * The mask used for any global/session overrides can be modified via `ExpandPvsEvent.Mask`.
* Toolshed Changes:
  * The signature of Toolshed type parsers have changed. Instead of taking in an optional command argument name string, they now take in a `CommandArgument` struct.
  * Toolshed commands can no longer contain a '|', as this symbol is now used for explicitly piping the output of one command to another. command pipes. The existing `|` and '|~' commands have been renamed to `bitor` and `bitnotor`.
  * Semicolon terminated command blocks in toolshed commands no longer return anything. I.e., `i { i 2 ; }` is no longer a valid command, as the block has no return value.

### New features

* The current map format/version has increased from 6 to 7 and now contains more information to try support serialization of maps with null-space entities and full game saves.
* `IEntitySystemManager` now provides access to the system `IDependencyCollection`.
* Toolshed commands now support optional and `params T[]` arguments. optional / variable length commands can be terminated using ';' or '|'.

### Bugfixes

* Fixed entity deserialization for components with a data fields that have a AlwaysPushInheritance Attribute
* Audio entities attached to invisible / masked entities should no longer be able to temporarily make those entities visible to all players.
* The map-like Toolshed commands now work when a collection is piped in.
* Fixed a bug in toolshed that could cause it to preferentially use the incorrect command implementation.
  * E.g., passing a concrete enumerable type would previously use the command implementation that takes in an unconstrained generic parameter `T` instead of a dedicated `IEnumeerable<T>` implementation.

### Other

* `MapChangedEvent` has been marked as obsolete, and should be replaced with `MapCreatedEvent` and `MapRemovedEvent.
* The default auto-completion hint for Toolshed commands have been changed and somewhat standardized. Most parsers should now generate a hint of the form:
  * `<name (Type)>` for mandatory arguments
  * `[name (Type)]` for optional arguments
  * `[name (Type)]...` for variable length arguments (i.e., for `params T[]`)


## 246.0.0

### Breaking changes

* The fixes to renderer state may have inadvertantly broken some rendering code that relied upon the old behavior.
* TileRenderFlag has been removed and now it's just a byte flag on the tile for content usage.

### New features

* Add BeforeLighting overlay draw space for overlays that need to draw directly to lighting and want to do it immediately beforehand.
* Change BlurLights to BlurRenderTarget and make it public for content usage.
* Add ContentFlag to tiles for content-flag usage.
* Add a basic mix shader for doing canvas blends.
* Add GetClearColorEvent for content to override the clear color behavior.

### Bugfixes

* Fix pushing renderer state not restoring stencil status, blend status, queued shader instance scissor state.


## 245.1.0

### New features

* Add more info to the AnchorEntity debug message.
* Make ParseObject public where it will parse a supplied Type and string into the specified object.

### Bugfixes

* Fix EntityPrototypeView not always updating the entity correctly.
* Tweak BUI shutdown to potentially avoid skipping closing.

### Other

* Increase Audio entity despawn buffer to avoid clipping.


## 245.0.0

### Breaking changes

* `BoundUserInterface.Open()` now has the `MustCallBase` attribute

### Bugfixes

* Fixed an error in `MappingDataNode.TryAddCopy()`, which was causing yaml inheritance/deserialization bugs.


## 244.0.0

### Breaking changes

* Increase physics speedcap default from 35m/s to 400m/s in-line with box2d v3.

### New features

* Add EntityManager overloads for ComponentRegistration that's faster than the generic methods.
* Add CreateWindowCenteredRight for BUIs.

### Bugfixes

* Avoid calling UpdateState before opening a BUI.


## 243.0.1

### Bugfixes

* Fixed `BaseWindow` sometimes not properly updating the mouse cursor shape.
* Revert `BaseWindow` OnClose ordering due to prior reliance upon the ordering.


## 243.0.0

### Breaking changes

* RemoveChild is called after OnClose for BaseWindow.

### New features

* BUIs now have their positions saved when closed and re-used when opened when using the `CreateWindow<T>` helper or via manually registering it via RegisterControl.

### Other

* Ensure grid fixtures get updated in client state handling even if exceptions occur.


## 242.0.1

### Bugfixes

* Fixed prototype reloading/hotloading not properly handling data-fields with the `AlwaysPushInheritanceAttribute`
* Fix the pooled polygons using incorrect vertices for EntityLookup and MapManager.

### Internal

* Avoid normalizing angles constructed from vectors.


## 242.0.0

### Breaking changes

* The order in which the client initialises networked entities has changed. It will now always apply component states, initialise, and start an entity's parent before processing any children. This might break anything that was relying on the old behaviour where all component states were applied before any entities were initialised & started.
* `IClydeViewport` overlay rendering methods now take in an `IRenderHandle` instead of a world/screen handle.
* The `OverlayDrawArgs` struct now has an internal constructor.

### New features

* Controls can now be manually restyled via `Control.InvalidateStyleSheet()` and `Control.DoStyleUpdate()`
* Added `IUserInterfaceManager.RenderControl()` for manually drawing controls.
* `OverlayDrawArgs` struct now has an `IRenderHandle` field such that overlays can use the new `RenderControl()` methods.
* TileSpawnWindow will now take focus when opened.

### Bugfixes

* Fixed a client-side bug where `TransformComponent.GridUid` does not get set properly when an existing entity is attached to a new entity outside of the player's PVS range.
* EntityPrototypeView will only create entities when it's on the UI tree and not when the prototype is set.
* Make CollisionWake not log errors if it can't resolve.

### Other

* Replace IPhysShape API with generics on IMapManager and EntityLookupSystem.

### Internal

* Significantly reduce allocations for Box2 / Box2Rotated queries.


## 241.0.0

### Breaking changes

* Remove DeferredClose from BUIs.

### New features

* Added `EntityManager.DirtyFields()`, which allows components with delta states to simultaneously mark several fields as dirty at the same time.
* Add `CloserUserUIs<T>` to close keys of a specific key.

### Bugfixes

* Fixed `RaisePredictiveEvent()` not properly re-raising events during prediction for event handlers that did not take an `EntitySessionEventArgs` argument.
* BUI openings are now deferred to avoid having slight desync between deferred closes and opens occurring in the same tick.


## 240.1.2


## 240.1.1

### Bugfixes

* Fixed one of the `IOverlayManager.RemoveOverlay` overrides not fully removing the overlay.


## 240.1.0

### New features

* Added an `AsNullable` extension method for converting an `Entity<T>` into an `Entity<T?>`

### Bugfixes

* Fixed an exception in `PhysicsSystem.DestroyContacts()` that could result in entities getting stuck with broken physics.

### Other

* `GamePrototypeLoadManager` will now send all uploaded prototypes to connecting players in a single `GamePrototypeLoadMessage`, as opposed to one message per upload.


## 240.0.1

### Bugfixes

* Fixed `SharedBroadphaseSystem.GetBroadphases()` not returning the map itself, which was causing physics to not work properly off-grid.


## 240.0.0

### Breaking changes

* `ComponentRegistry` no longer implements `ISerializationContext`
* Tickrate values are now `ushort`, allowing them to go up to 65535.

### New features

* Console completion options now have new flags for preventing suggestions from being escaped or quoted.
* Added `ILocalizationManager.HasCulture()`.
* Static `EntProtoId<T>` fields are now validated to exist.

### Bugfixes

* Fixed a state handling bug in replays, which was causing exceptions to be thrown when applying delta states.

### Other

* Reduced amount of `DynamicMethod`s used by serialization system. This should improve performance somewhat.

### Internal

* Avoided sorting overlays every render frame.
* Various clean up to grid fixture code/adding asserts.

## Older versions

Release notes for older versions have been culled due to file size, see [here](https://github.com/space-wizards/RobustToolbox/blob/dc1464b462911afc84992d0858575fc23e611c3f/RELEASE-NOTES.md) for them.
