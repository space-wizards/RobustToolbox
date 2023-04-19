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

*None yet*

### Bugfixes

*None yet*

### Other

*None yet*

### Internal

*None yet*


## 0.96.9.0

### New features

* `RobustIntegrationTest` now has a `DoGuiEvent()` method that can directly pass `GUIBoundKeyEventArgs` to a control. 


## 0.96.8.2

### New features

* The `LayerSetData()` function can now be used to clear a sprite layer's shader.

### Bugfixes

* Fixed sandboxing verifying against `Robust.` assemblies inside `Robust.Client.WebView`, causing an older assembly to be verified against.


## 0.96.8.1

### Bugfixes

* Fix MapInit not being run on entities in some instances.


## 0.96.8.0

### Bugfixes

* Create entities before applying entity states. This fixes parenting issues in some instances, for example on a freshly split grid the client would give an exception.

### Other

* Entities have their paused state set before initialisation rather than after.

### Internal

* Added a BroadphaseNetworkingTest.


## 0.96.7.0

### New features

* `IDynamicTypeFactory.CreateInstance` now has the option to not perform dependency injection.
* Added normal blend mode for shaders
* Added a new ResPath struct that is intended to eventually replace ResourcePath

### Bugfixes

* Hopefully fixed an IndexOutOfRange exception in AudioSystem
* Fixed a potential IndexOutOfRange exception in ContainerSystem


## 0.96.6.0

### New features

* Added overrides to shuffle Span<T> and ValueList<T> in IRobustRandom.
* Added hotkeys to close the most recent window and all windows.

### Other

* Improved some container assert messages.


## 0.96.5.0

### New features

* Added source generator for automatically generating component state getting & handling code. Significantly reduces boilerplate when creating networked components.


## 0.96.4.0

### Bugfixes

* Component delta states can now have an initial full state inferred by clients.


## 0.96.3.0

### Other

* Updated server SQLitePCLRaw to 2.1.4.


## 0.96.2.0


## 0.96.1.0

### New features

* Implemented deleting a full word at a time.

### Bugfixes

* Fixed `ContainerSystem.EmptyContainer` sometimes failing to empty containers.
* Fixed container state handling sometimes failing to insert or remove entities.
* Fix content test workflow.
* Text contents won't draw over the scrollbar for OutputPanel controls anymore.
* Invalidate OutputPanel entries upon it entering the UI tree. This fixes some bugs where text is added while it's outside of the tree without the UI scale cvar being set causing separate sizings in entries.


## 0.96.0.4

### Bugfixes

* Revert InRange entity lookup range change due to content bugs.
* Fix implicit appearance state data.


## 0.96.0.3

### Bugfixes

* Fix sprite error log to report the key not the layer.
* Fix log length for physics contact error.
* Fix discord null errors.
* Adjust InRange lookups to check if the centre of body is in range.

### Other

* Add more audio logs.


## 0.96.0.2

### Bugfixes

* Fix adding MapGridComponent to a map with pre-existing child entities.


## 0.96.0.1

### Other

* Set blend function for shaders with ShaderBlendMode.None
* Add logs around fixture lengths in contact updates.
* Revert previous contact changes to try to make physics slightly more stable until Box2D 3.0.
* Adjusted QueueDeleteEntity log on client to care if the entity is deleted in prediction.


## 0.96.0.0

### Breaking changes

* Removed `MapId` serializer. Serialize the map's EntityUid instead.
* Renamed `MapComponent.WorldMap` to `MapComponent.MapId`.

### New features

* Added showrot command as a counterpart to showpos.

### Other

* Added error logs when QueueDel is called on the client for networked entities.
* Added logs around physics contact errors that have been happening.


## 0.95.0.0

### Bugfixes

* Reverted making `MetaDataComponent.PauseTime` a yaml data-field, as it caused issues when saving uninitialised maps.

### Internal

* `TextEdit`'s `NextWordPosition` has been replaced with `EndWordPosition`


## 0.94.0.0

### Breaking changes

* `IGameTiming.IsFirstTimePredicted` is now false while applying game states.

### Bugfixes

* `MetaDataComponent.PauseTime` is now a yaml data-field
* The client-side `(un)pausemap` command is now disabled while connected to a server.

### Internal

* Use a List<Contact> for contacts instead of a shared arraypool to try to fix the contact indexing exception.
* Moved IoC dependencies off of physics contacts.


## 0.93.3.0

### New features

* Unnecessary tiles are no longer written to map file tilemaps.
* Added the ability to enable or disable grid splitting per grid.

### Other

* Added additional logs around contact issue


## 0.93.2.0

### New features

* Add CompletionHelpers for components and entityuids.


## 0.93.1.0

### New features

* Add PlayPredicted audio method for EntityCoordinates.

## 0.93.0.0

### Breaking changes

* Arguments of ContainerSystem's `EmptyContainer()` have changed. It now also returns removed entities.

### New features

* Added a TerminatingOrDeleted() helper function
* Added a `hub_advertise_now` command.

### Bugfixes

* Fixed some multi-threading IoC errors in the audio system.
* The map validator now allows entities to specify missing components.
* Fixed a potential stack overflow in the colour slider control.
* Fixed sprites sometimes not updating `IsInert`.

### Other

* `TransformComponentAttachToGridOrMap()` is now obsoleted. use the newly added system method instead.
* Made RSI preloading more error toletant.
* Added some new benchmarks for testing archetype ECS.


## 0.92.2.1

### Bugfixes

* Revert tile bound shrinkage as it was causing erroneous test failures on content.


## 0.92.2.0

### New features

* Added Box2iEdgeEnumerator for iterating its bounds.
* Added a CompletionResult helper for MapIds
* Added some helper methods for System.Random (useful for seeded RNG)

### Bugfixes

* Shrink tile bounds by 0.05. In some cases the polygon skin radius was causing overlap on other tiles and leading to erroneous lookup r
* Use preset matrixes for certain Matrix3 angles to avoid imprecision issues with transformations.


## 0.92.1.0

### New features

* Add option to SplitContainer for which split expands on parent resize

### Internal

* Updated Lidgren to v0.2.4.


## 0.92.0.0

### New features

* Exposed more properties on `FastNoiseLite`.
* Added fallback culture for localization.

### Bugfixes

* Fixed noise DD.

### Other

* Added new `DebugOpt` and `Tools` build configurations. These must be added to your solution file and apply to all projects importing `Robust.Properties.targets`.
  * `DebugOpt` is "`Debug` with optimizations enabled".
  * `Tools` has development tools (e.g. `launchauth` command) that release builds don't, while still having asserts (`DEBUG`) off and optimizations on.
* All configurations except `Release` now define `TOOLS`.
* `Release` is now intended to be "as close to published release as possible" with game configuration. Use `Tools` as build configuration instead for scenarios such as mapping.
* `Robust.Properties.targets` should now be included at the end of project files. `Robust.Analyzers.targets` and `Robust.DefineConstants.targets` are now included by it automatically.

### Internal

* General cleanup to MSBuild files.

## 0.91.0.0

### Breaking changes

* `ColorSelectorSliders` now uses SpinBox instead of FloatSpinBox.

### New features

* `IntegrationOptions` now allows changing the `ILogHandler` used by the integration test via `OverrideLogHandler`.

### Bugfixes

* Default integration test log output should more reliably capture `TestContext.Out` now.


## 0.90.0.0

### Breaking changes

* Add tile edge rendering support.

### New features

* Add .AsUint() for ValueDataNode.

### Bugfixes

* Fix AnchorEntity replication when the coordinate doesn't change
* Fix some PVS bugs.
* Fix rounding in GetGridOrMapTilePosition.


## 0.89.1.0

### New features

* `web.headless` CVar can now be used to avoid loading CEF with graphical client.

### Bugfixes

* `web.user_agent` CVar can now be overriden by content before WebView is initialized.

### Other

* WebView works again and is properly available from the launcher.

### Internal

* Clean up WebView initialization logic to avoid static `IoCManager`.


## 0.89.0.0

### Breaking changes

* Add EntityUid as an arg to SharedTransformSystem and remove more .Owner calls.

### New features

* Add by-ref event analyzer.
* Add option to hide scrollbars for ScrollContainers.
* Add an out EntityUid overload to EntityQueryEnumerator<T>.

### Bugfixes

* Fix exception on server shutdown.
* Fix concurrent update error in byref registrations for serializationmanager.
* New grids created from placement manager start at 0,0 rather than -1,-1.

### Other

* `dump_netserializer_type_map` command to debug desynchronization issues with NetSerializer's type map.


## 0.88.1.0

### New features

* Added a new OnScreenChanged event that gets invoked when `IUserInterfaceManager.ActiveScreen` changes.
* UI state interfaces such as `IOnStateEntered<TState>` now also get invoked whenever the current state inherits from `TState`.

### Bugfixes

* Fixed `WritableDirProvider.Find()`. This fixes custom MIDI soundfonts on Windows.
* Fixed server startup crash with string serializer length checks.
* Fixed `CS8981` errors in `Robust.Benchmarks`.
* Fixed C# interactive errors when engine started without content-start.
* Fixed FormattedMessage.IsEmpty() returning the wrong result.

### Other

* Map pausing now gets properly networked
* SplitContainers controls now have a minimum draggable area, so that they can function without any padding.

### Internal

* Fixed `CS8981` errors in `Robust.Benchmarks`.


## 0.88.0.0

### Breaking changes

* A `Default` font prototype is now required. I.e.:
    ```yaml
    - type: font
      id: Default
      path: /Fonts/NotoSans/NotoSans-Regular.ttf
    ```

### New features
* `FormattedText.MarkupParser` got refactored to be more robust and support arbitrary tags.
* New rich text tags can be added by implementing `IMarkupTag`



## 0.87.1.1

### Bugfixes

* Fixed source of PVS assert tripping in debug.


## 0.87.1.0

### Bugfixes

* Fixed a PVS bug that would sometimes cause it to attempt to send deleted entities.
* Fixed server commands not getting sent to clients after disconnecting and reconnecting.
* Fixed a text input error when using the right arrow key while at the second to last character.


### Other

* Sprite view controls now use the sprite's offset when rendering.
* The sprite system should now animate any rendered sprites with RSI animations, instead of only animating those visible in the main viewport and sprite view controls.


## 0.87.0.0

### Breaking changes

* `UIScreen.GetOrNewWidget()` has been replaced with `GetOrAddWidget()`.

### New features

* Added `IWritableDirProvider.OpenSubdirectory()`, which returns a new `IWritableDirProvider` with the root set to some subdirectory.
* Added `UiScreen.TryGetWidget()`
* Added a virtual `Shutdown()` method for game/module entry points.

### Bugfixes

* Fixed SyncSpriteComponent not properly syncing entities that are out of view.
* Fixed a bug preventing client-side commands from being properly registered.
* Fixed a bug causing PVS to unnecessarily send extra data.


## 0.86.0.0

### Breaking changes

* Undid `*.yaml` prototype loading change from previous version.
* `IConsoleHost`'s `RegisteredCommands` field has been renamed to `AvailableCommands`.
* Several light related cvars have been renamed. E.g., "display.softshadows" is now "light.softshadows".
* The "display.lightmapdivider" integer cvar has been replaced with a float multiplier named "light.resolution_scale".


### New features

* Command definitions have a new bool that restricts them to only be executable by the server or in single player mode. Several "server only" commands have been moved to to shared code and now use this option.
* The FOV color is now configurable via the "render.fov_color" cvar

### Bugfixes

* SDL2 backend now works if the client is started with fullscreen.

### Other

* SDL2 backend now handles quit events (⌘+Q on macOS).
* SDL2 backend now logs video driver backend used on initialization.
* The engine will now warn on startup if `*.yaml` files are found in resources, as this most likely indicates an accident.
* Added entity, occluder and shadow-casting light counts to the clyde debug panel.
* The HistoryLineEdit control now invokes `OnTextChanged` events when selecting history items

### Internal

* Changed thread safety around `ResourceManager`'s VFS roots, removing the use of error prone reader-writer locks.
* SDL2 log now shows log category.
* Removed OpenTK DllMap code.


## 0.85.2.0

### New features

* Threaded windowing API usage is now behind a CVar, disabled by default on macOS to avoid crashes.
* Box2i, ImmutableHashSet, ISet, and IReadonlySet can now be serialized.
* Added helpers for Box2i Center / Vector2i Up-Down-Left-Right.
* Implement blend modes for rendering.

### Bugfixes

* MacOS with the SDL2 backend now has DPI scaling enabled.
    * Fixed DPI scaling calculations on platforms outside Windows.
* Grids on top of maps that are also grids should render correctly now.
* Fixed bug in ScrollContainer that could cause permanent loops.
* Fixed occluder tree error.
* Fixed Texture.GetPixel.

### Other

* System F3 panel now correctly fetches processor model on Apple Silicon devices.
* UI content scale is now listed in the F3 coordinates panel.
* SDL2 backend is now wired up to update key names dynamically on keyboard mode change.
* The prototype reload event is no longer wrapped under #if !FULL_RELEASE.
* The engine now loads `*.yaml` files (previously loading only `*.yml`) for prototypes.

### Internal

* `keyinfo` command has enum completions.

## 0.85.1.1

### Bugfixes

* Fixed GameStateManager error when resetting client-side prediction


## 0.85.1.0

### New features

* RSI's now get combined into a large atlas.

### Bugfixes

* Removed bad PlayAudioPositionalMessage error log & fixed fallback coordinate check.
* Fixed MouseJoint parallelisation exception.

### Internal

* Fixed some warnings in GameStateManager


## 0.85.0.1

### Bugfixes

* Fix fixture client state handling not removing the existing fixture.
* Use a dummy entity for placement manager preview so offsets are applied correctly.


## 0.85.0.0

### Breaking changes

* Component.Shutdown() has now been removed and the eventbus should be used in its place.
* Component.Name has now been removed and IComponentFactory.GetComponentName(Type) should be used in its place.

### Bugfixes

* Ensure fixture contacts are destroyed even if no broadphase is found.
* Ensure fixtures are re-created in client state handling. There was a subtle bug introduced by updating existing ones where contacts were incorrectly being retained across prediction. This was most obvious with slipping in SS14.


## 0.84.0.0

### Breaking changes

* EffectSystem has been removed.

### New features

* Added Pidgin parser to the sandbox whitelisted.

### Bugfixes

* Fixed physics ignoring parallelisation cvars
* Global audio volume is no longer overridden every tick.
* Fix `SpriteComponent.CopyFrom()` not working properly.
* Fix cvar TOML parsing failing to read some numeric cvars.

### Other

* Improved physics joint logging.


## 0.83.0.0

### Breaking changes

* Physics has been ECSd with large API changes:
- Shapes can be updated via the system rather than requiring the caller to handle it.
- Access attributes have been added.
- Implemented IEquatable for Fixture Shapes
- Removed obsolete PhysicsComponent APIs.
- Removed usage of Component.Owner internally.


## 0.82.0.0

### Breaking changes

* `Box2Rotated.Centre` has been renamed to `.Center`
* `ISpriteComponent` has been removed. Just use `SpriteComponent` instead.

### Bugfixes

* Fixed prototype reloading/uploading.
* Fixed UI tooltips sometimes causing a null reference exception.

### Other

* Map/world velocity calculations should be slightly faster.
* `EnsureComp` will now re-add a component if it has been queued for removal.


## 0.81.0.0

### Breaking changes

* TransformComponent,Parent has been removed. Use the ParentUid & get the component manually.

### New features

* The Popup control now has an OnPopupOpen event.

### Other

* Various transform methods are now obsolete. Use the methods provided by the transform system instead.
* TransformComponent.MapUid is now cached (previously required a dictionary lookup)


## 0.80.2.0

### New features

* Tooltips now provide the option to track the mouse cursor.


## 0.80.1.0

### New features

* Added location of compile errors to XAML UI.
* Add CC-BY to RSI.json
* Allow customising radio buttons for RadioOptions.
* Added CVar to override CEF useragent.

### Bugfixes

* Fix incorrect size of second window in split container.
* Fix PreventCollideEvent fixture ordering.

### Other

* Obsoleted .Owner for future work in removing components storing a reference to their entityuid.


## 0.80.0.0

### Breaking changes

* Moved ConvexHullPolygons and MaxPolygonVertices cvars to constants.
* Moved the PhysicsMap Gravity property to its own controller.
* Made some layout changes to Split Container.

### New features

* Added the colliding fixtures to PreventCollideEvent.

### Bugfixes

* Grids overlapping entities will now flag the entity for grid traversal.

### Other

* The split container `Measure()` override now more accurately reflects the space available to children. Additionally, the split position is now publicly settable.

### Internal

* Removed manual component registrations.


## 0.79.0.1

### New features

* Add helper GetDirection to SharedMapSystem that offsets a Vector2i in the specified direction by the specified distance.
* UIController now implements IEntityEventSubscriber

### Bugfixes

* The fast TryFindGridAt overload will now also return the queried map's MapGridComponent if it exists.

### Other

* Updated window dragging movement constraints. By default windows can now be partially dragged off-screen to the left. This is configurable per window. This also fixes a bug where windows could become unreachable.

### Internal

* Remove 2 TryGetComponents per physics contact per tick.


## 0.79.0.0

### Breaking changes

* EntityInitializedMessage has been removed; the C# event invoked on EntityManager (EntityInitialized) should be used in its place.
* TileChangedEventArgs has been removed.

### Bugfixes

* Fix tooltip panels being incorrectly sized for their first frame.
* Client will no longer predict physics sleeping on bodies that are unable to sleep.
* Style box texture scaling has been fixed.

### Other

* Added TaskCompletionSource to the sandbox.

### Internal

* IPhysManager has been removed for a slight physics contacts optimisation.
* Optimise TryFindGridAt, particularly for grid traversals.
* MapGridComponent now uses delta component states.
* Removed some TryGetComponent from IsMapPaused, speeding up entity initialization in some instances.


## 0.78.0.0

### Breaking changes

* Removed the obsoleted `GlobalLinearVelocity()` EntityUid helper method.
* INetConfigurationManager now has client & server side variants. Clients can now properly set server authoritative cvars when in singleplayer mode
* IPhysBody has been removed. Just use the physics component.
* Physics joints haven been slightly refactored and some method signatures have changed.

### New features

* Added a new cvar to limit audio occlusion raycast lengths ("audio.raycast_length").
* IRobustSerializer has new public methods for getting hashes and setting string serializer data.

### Bugfixes

* Fixed broken click bound checks in the `Tree` UI Control.
* Removed erroneous debug assert in render code that was causing issued in debug mode.
* Fixed some instances where rotation-less entities were gaining non-zero local rotation.

### Other

* Tickrate is now shown in the f3 debug monitors


## 0.77.0.2

### New features

* Scroll containers now have public methods to get & set their scroll positions.

### Bugfixes

* Fixed entity spawn menu sometimes not properly updating when filtering entities.

### Other

* Physics contacts are now stored per-world rather than per-map. This allows the multi-threading to be applicable to every contact rather than per-map.
* Contacts will no longer implicitly be destroyed upon bodies changing maps.


## 0.77.0.1

### Bugfixes

* Fix AttachToGridOrMap not retaining an entity's map position.


## 0.77.0.0

### Breaking changes

* ClientOccluderComponent has been removed & OccluderComponent component functions have been moved to the occluder system.
* The OccluderDirectionsEvent namespace and properties have changed.
* The rendering and occluder trees have been refactored to use generic render tree systems.
* Several pointlight and occluder component properties now need to be set via system methods.
* SharedPhysicsMap and PhysicsMap have been combined.
* RunDeferred has been removed from transformcomponent and updates are no longer deferred.

## 0.76.0.0

### Breaking changes

* Physics contact multi-threading cvars have been removed as the parallelism is now handled by IParallelManager.

### New features

* Physics now supports substepping, this is under physics.target_minimum_tickrate. This means physics steps will run at a constant rate and not be affected by the server's tickrate which can reduce the prevalence of tunneling.
* FastNoise API is now public.

### Other

* UPnP port forwarding now has better logging.
* Physics solver has been refactored to take more advantage of parallelism and ECS some internal code.
* Sprite processing & bounding box calculations should be slightly faster now.
* Nullspace maps no longer have entities attached.


## 0.75.1.0

### New features

* Serv4's notNullableOverride parameter is now enforced by analyzer. For more info, see [the docs](https://docs.spacestation14.io/en/engine/serialization).
* Added command to dump injector cache list.

### Bugfixes

* Fix generic visualisers not working because of recent appearance system changes in v0.75.0.0
* Fix physics not working properly on moving grids (transform matrix deferral).

### Other

* Transform matrix dirtying is deferred again (undo change in v0.75.0.0
* Added two new serv3 analysers (NotNullableFlagAnalyzer and PreferGenericVariantAnalyzer)


## 0.75.0.0

### Breaking changes

* Changed default for `net.buffer_size` to `2`.
* Changed default for `auth.mode` to `Required`. On development builds, the default is overriden to remain at `Optional`, so this only affects published servers.
* The default value for the `outsidePrediction` argument of the `InputCmdHandler.FromDelegate()`  has changed from false to true.

### New features

* Appearance system now has generic `TryGetData<T>()` functions.

### Bugfixes

* Mapped string serializer once again is initialized with prototype strongs, reducing bandwidth usage.
* Fixed various keybindings not working while prediction was disabled.
* Fixed a bug causing rendering trees to not properly recursively update when entities move.

### Other

* Transform matrix dirtying is no longer deferred.
* Cleaned up some `FULL_RELEASE` CVar default value overrides into `CVarDefaultOverrides.cs`.
* VVRead now attempts to serialize data to yaml


## 0.74.0.0

### Breaking changes

* `ITypeReader<,>.Read(...)` and `ITypeCopier<>.Copy(...)` have had their `bool skipHook` parameter replaced with a `SerializationHookContext` to facilitate multithreaded prototype loading.
* Prototypes are now loaded in parallel across multiple threads. Type serializers, property setters, etc... must be thread safe and not rely on an active IoC instance.

### Bugfixes

* Mapped string serializer once again is initialized with prototype strongs, reducing bandwidth usage.

### Other

* Drastically improved startup time by running prototype loading in parallel.
  * `AfterDeserialization` hooks are still ran on the main thread during load to avoid issues.
* Various systems in the serialization system such as `SerializationManager` or `ReflectionManager` have had various methods made thread safe.
* `TileAliasPrototype` no longer has a load priority set.
* Straightened out terminology in prototypes: to refer to the type of a prototype (e.g. `EntityPrototype` itself), use "kind".
  * This was previously mixed between "type" and "variant".

### Internal

* `SpanSplitExtensions` has been taken behind the shed for being horrifically wrong unsafe code that should never have been entered into a keyboard ever. A simpler helper method replaces its use in `Box2Serializer`.
* `PrototypeManager.cs` has been split apart into multiple files.

## 0.73.0.0

### Breaking changes

* The entity lookup flag `LookupFlags.Anchored` has been replaced with `LookupFlags.Static`.
* We are now using **.NET 7**.
* `IDependencyCollection`/`IoCManager` `RegisterInstance` does not automatically add the instance to object graph, so `BuildGraph()` must now be called to see the new instances.
  * `deferInject` parameteres have been removed.

### New features

* The server will now check for any unknown CVars at startup, to possibly locate typos in your config file.
* `IDependencyCollection` is now thread safe.

### Bugfixes

* Fixed config files not being truncated before write, resulting in corruption.

### Other

* Removed some cruft from the `server_config.toml` default config file that ships with Robust.
* Most usages of x86 SIMD intrinsics have been replaced with cross-platform versions using the new .NET cross-platform intrinsics.
  * This reduces code to maintain and improves performance on ARM.
* Tiny optimization to rendering code.
* `RobustSerializer` no longer needs to be called from threads with an active IoC context.
  * This makes it possible to use from thread pool threads without `IoCManager.InitThread`.
* Removed finalizer dispose from `Overlay`.
* Stopped integration tests watching for prototype reload file changes, speeding stuff up.

### Internal

* Moved `SerializationManager`'s data definition storage over to a `ConcurrentDictionary` to improve GC behavior in integration tests.

## 0.72.0.0

### Breaking changes

* EntityPausedEvent has been split into EntityPausedEvent and EntityUnpausedEvent. The unpaused version now has information about how long an entity has been paused.

## 0.71.1.4

### Bugfixes

* Fixed CVars not being saved correctly to config file.

### Other

* Mark `validate_rsis.py` as `+x` in Git.
* Made config system more robust against accidental corruption when saving.


## 0.71.1.3


## 0.71.1.2

### Bugfixes

* Fixed UI ScrollContainer infinite loop freezing client.


## 0.71.1.1

### Bugfixes

* Fixed client memory leaks and improved performance in integration testing.


## 0.71.1.0

### New features

* Better RSI validator script.
* When a new map file is loaded onto an existing map the entities will be transferred over.
* Add an API to get the hard layer / mask for a particular physics body.

### Bugfixes

* Fixed non-filled circle drawing via world handle.
* Fix max_connections in the default server config.
* Fix removal of PVS states for players without ingame status.
* Fix max rotation from the physics solver.

### Internal

* Wrap window rendering in a try-catch.


## 0.71.0.0

### Breaking changes

* `DebugTimePanel`, `DebugNetPanel` and `DebugNetBandwidthPanel` have been made internal.
* RSIs with trailing commas in the JSON metadata are no longer allowed.

### Bugfixes

* `csi` doesn't throw a `NullReferenceException` anymore.

### Other

* The `game.maxplayers` CVar has been deprecated in favor of the new `net.max_connections` CVar. Functionality is the same, just renamed to avoid confusion. The old CVar still exists, so if `game.maxplayers` is set it will be preferred over the new one.
* The new default for `net.max_connections` is 256.
* Debug monitors (F3) now have margin between them.
* F3 (clyde monitor) now lists the windowing API and version in use.
* Added system monitor to F3 with various info like OS version, .NET runtime version, etc...
* The engine now warns when loading `.png` textures inside a `.rsi`. This will be blocked in the future.


## 0.70.0.0

### New features

* `game.desc` CVar for a server description to show in the launcher.
* New system for exposing links to e.g. a Discord in the launcher.
  * The engine does not have a built-in method for configuring these, but it does now have a `StatusHostHelpers.AddLink` method to correctly format these from content. The idea is that content wires the types of links (with icon names) up itself via `IStatusHost.OnInfoRequest`.
  * See also [the HTTP API documentation](https://docs.spacestation14.io/en/engine/http-api) for reference.
* `GameShared` now has a `Dependencies` property to allow access to the game's `IDependencyCollection`. This makes it possible to avoid using static `IoCManager` in `EntryPoint`-type content code.
* A new define constant `DEVELOPMENT` has been defined, equivalent to `!FULL_RELEASE`. See [the docs](https://docs.spacestation14.io/en/technical-docs/preprocessor-defines) for details.
* `IConfigurationManager` has new functions for reading and writing CVar directly from a TOML file `Stream`.
* New `IConfigurationManager.LoadDefaultsFromTomlStream` to load a TOML file as CVar default overrides.
* Added new serializers to support Queue<T> data-fields.
* Added a `FromParent()` function to `IDependencyCollection`, enabling dependencies to be passed to parallel threads.
* `IClientStateManager` now has a `PartialStateReset()` function to make it easier for content to rewind to previous game states.
* Added `IClientNetManager.DispatchLocalNetMessage()`, which allows a client to raise a local message that triggers networked event subscriptions.

### Bugfixes

* `IPlayerSession.OnConnect()` now actually gets called when players connect.
* `MapLoaderSystem.TryLoad(.., out rootUids)` now properly only returns entities parented to the map.

### Other

* Invalid placement types for the entity spawn menu now log warnings.
* Slightly improved sprite y-sorting performance.

### Internal

* The current physics map that an entity is on is now cached in the transform component alongside other cached broadphase data. This helps to fix some broadphase/lookup bugs.

## 0.69.0.0


## 0.68.0.0

### Breaking changes

* Updated yml schema validator to remove the `grids` node.

### Bugfixes

* Fixed position-less audio playing.
* Stop mapgrids from serializing their fixtures.

### Other

* Removed the `restart` command, since it never worked properly and just confused people.
* Add virtual to some UIScreen methods.
* Add public parameterless ctor to MenuBar.


## 0.67.2.2

### Bugfixes

* Fix double MapGrid chunk subscription.
* Fix grid contacts short-circuiting collision.


## 0.67.2.1

### Bugfixes

* Fix MapChunks not being subscribed to by MapGridComponents in some instances.


## 0.67.2.0

### New features

* Add submenu support to menubar controls.

### Bugfixes

* Fix gridtree returning mapgrid maps twice.


## 0.67.1.3

### Bugfixes

* Fix Map regression so now they can be MapGrids again without the client crashing.


## 0.67.1.2

### Bugfixes

* Fix some mapgrids not being marked as dirty and never being sent to clients (thanks checkraze).


## 0.67.1.1

### Bugfixes

* Fix some merge artifacts from mapgrid support for maps.


## 0.67.1.0

### New features

- Maps can now have MapGridComponent added to them.


## 0.67.0.0

### Breaking changes

* MapGrid is deprecated and has been merged into MapGridComponent. This is subject to further changes as it gets ECSd more in future.
* The `grids` yaml node on map files is deprecated and has been merged onto MapGridComponent. Loading maps is backwards compatible for now but is subject to change in future. Saving maps will save in the new format.


## 0.66.0.0

### Breaking changes

* AudioSystem functions for playing audio have changed. Functions that take in filters now require an additional argument that will determine whether sounds are recorded by replays. Additionally, there are several new overrides that take in a recipient session or entity.

### Bugfixes

* Script globals for C# interactive were not having dependencies injected correctly.
* GetWorldPosition() now returns the correct positions even prior to transform initialization.
* Fix map loading not properly offsetting some entities that were directly parented to the map.

### Internal

* Added lookup/broadphase re-parenting tests.


## 0.65.2.1

### Bugfixes

* Fix empty MetaData components being serialized to map files.
* Fix saving a grid as a map not marking it as pre-mapinit.

### Other

* Set `ValidateExecutableReferencesMatchSelfContained` in the server project, which may help with publishing issues. I hope.
* Move pinned font data over to Pinned Object Heap.
* Improved shader code generation for uniform arrays to be more compatible.
* Server now has server GC enabled by default.

### Internal

* Remove some unnecessary dependency resolves from filters making audio much more performant.


## 0.65.2.0

### New features

* Added ClydeAudio.StopAllAudio()
* Expose more tick logic to content.

### Bugfixes

* Fix bad reference in WebView.

### Internal

* Add Robust.Packaging to solution.
* Add WebView to solution.
* Physics contacts are now parallel and much faster.

## 0.65.1.0

### New features

* Implement value prototype id dictionary serializer.

### Bugfixes

* Fixes lerping clean up issue added in #3472.

### Internal

* Add test for (de)serializing data record structs.


## 0.65.0.1

### Bugfixes

- Fix SetLocalPositionRotation raising 2 moveevents. This should help physics performance significantly.
- Fix tpgrid responses and command error.


## 0.65.0.0

### Breaking changes

* Rename transform lerping properties alongside other minor internal changes.

### Bugfixes

* Fix physics testbeds.
* Force grids to always be collidable for now and stop them clipping.

### Other

* Slight optimization to `OutputPanel`'s handling of internal `RichTextEntry`s.
* Force non-collidable contacts to be destroyed. Previously these hung around until both entities became collidable again.

### Internal

* `Tools/version.py` has been updated to automatically update `RELEASE-NOTES.md`.
* General cleanup to `Tools/version.py`.

## 0.64.1.0

### Bugfixes

* Word-wrapping in `OutputPanel` and `RichTextLabel` has been fixed.

## 0.64.0.0

### Breaking changes

* IMapLoader has been refactored into MapLoaderSystem. The API is similar for now but is subject to change in the future.

## 0.63.0.0

### Breaking changes

* Thanks to new IME support with SDL2, `IClyde.TextInputStart()` and `IClyde.TextInputStop()` must now be appropriately called to start/stop receiving text input when focusing/unfocusing a UI control. This restriction is applied even on the (default) GLFW backend, to enforce consistent usage of these APIs.
* `[GUI]TextEventArgs` have been renamed to `[GUI]TextEnteredEventArgs`, turned into records, and made to carry a `string` rather than a single text `Rune`.
* IoC and `DependencyCollection` `Register` methods now have a `TInterface : class` constraint.
* [ABI] `IoCManager.InitThread` now returns the `IDependencyCollection`.

### New features

* Fixes for compiling & running on .NET 7. You'll still have to edit a bunch of project files to enable this though.
* `FormattedMessage.EnumerateRunes()`
* `OSWindow.Shown()` virtual function for child classes to hook into.
* `IUserInterfaceManager.DeferAction(...)` for running UI logic "not right now because that would cause an enumeration exception".
* New `TextEdit` control for multi-line editable text, complete with word-wrapping!
* `Rope` data structure for representing large editable text, used by the new `TextEdit`.
* Robust now has IME support matching SDL2's API. This only works on the SDL2 backend (which is not currently enabled by default) but the API is there:
    * `IClyde.TextInputStart()`, `IClyde.TextInputStop()`, `IClyde.TextInputSetRect()` APIs to control text input behavior.
    * `TextEditing` events for reporting in-progress IME compositions.
    * `LineEdit` and `TextEdit` have functional IME support when the game is running on SDL2. If you provide a font file with the relevant glyphs, CJK text input should now be usable.
* `Register<T>` (single type parameter) extension method for `IDependencyCollection`.

### Bugfixes

* Fixes erroneous literal "\\n" inside the Clyde debug panel.
* Fixed Lidgren connection status changes potentially getting mislogged.
* Fixed missing components not being correctly saved for maps
* Fixed map saving sometimes not including new components.
* Fix hot reload unit tests.

### Other

* Properly re-use `HttpClient` in `NetManager` meaning we properly pool connections to the auth server, improving performance.
* Hub advertisements have extended keep-alive pool timeout, so the connection can be kept active between advertisements.
* All HTTP requests from the engine now have appropriate `User-Agent` header.
* `bind` command has been made somewhat more clear thanks to a bit of help text and some basic completions.
* `BoundKeyEventArgs` and derivatives now have a `[DebuggerDisplay]`.
* Text cursors now have a fancy blinking animation.
* `SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH` is set on the SDL2 windowing backend, so clicking on the game window to focus it will pass clicks through into the game itself, matching GLFW's behavior.
* Windows clipboard history paste now works.
* Improved multi-window UI keyboard focusing system: a single focused control is now tracked per UI root (OS window), and is saved/restored when switching between focused window. This means that you (ideally) only ever have a UI control focused on the current OS window.

### Internal

* `uitest2` is a new command that's like `uitest` but opens an OS window instead. It can also be passed an argument to open a specific tab immediately.
* Word-wrapping logic has been split off from `RichTextEntry`, into a new helper struct `WordWrap`.
* Some internal logic in `LineEdit` has been shared with `TextEdit` by moving it to a new `TextEditShared` file.
* SDL2 backend now uses `[UnmanagedCallersOnly]` instead of `GetFunctionPointerForDelegate`-style P/Invoke marshalling.
* Entity prototype reloading logic has been moved out of `PrototypeManager` and into a new `PrototypeReloadSystem`.
* Most usages of `IoCManager.` statically have been removed in favor of dependency injection.

## 0.62.1.0

### Bugfixes

* Fixed a PVS issue causing entities to be sent to clients without first sending their parents.
* Improved client-side state handling exception tolerance.

### Other

* Removed null-space map entities.

### Internal

* Added some more anchoring tests.

## 0.62.0.1

### Bugfixes

* Fixed sprites not animating when directly toggling layer visibility,
* Fixed anchored entities not being added to the anchored lookups.

## 0.62.0.0

### Breaking changes

* Removed some obsolete map event handlers.

### New features

* Added entity query struct enumerators

### Bugfixes

* Improved error tolerance during client state application.
* Added better error logs when a client deletes a predicted entity.
* Fixes command permissions not getting sent to clients.
* Fixes a broad-phase bug were entities were not properly updating their positions.

### Other

* Added the LocalizedCommands class, which automatically infer help and description loc strings from the commands name.

## 0.61.0.0

### Breaking changes

* IMap and IMapGrid have been removed. Just use the associated components directly.

### Other

* AudioSystem has been refactored.

## 0.60.0.0

### Breaking changes

* ISerializationHooks.BeforeSerialization() has been removed. Use custom type serializers instead.

### New features

* Added function to UserInterfaceSystem that returns list of BUIs that a client has open.

### Bugfixes

* Fixed various container related broadphase bugs which could result in entities getting stuck with a null-broadphase.
* Fixed client fixture state handling bug that caused the client to incorrectly disable collision.

### Other

* Misc PVS optimisations

### Internal

* Removed redundant grid-init physics logic
* Modified garbage collection for entity spawning profiling.

## 0.59.0.0

### Breaking changes

* Various transform related methods have been removed from MapGrids
* TransformSystem.SetCoordinates() arguments have changed and now allow an entity to be sent to nullspace

### Bugfixes

* Fixed an entity lookup bug that sometimes failed to return entities in StaticSundriesTrees

### Other

* The EntitySystem.Resolve<> methods have been change to protected

## 0.58.1.1

### Bugfixes

* Fixed some container shutdown errors
* Fixed LookupFlags.Static not acting as a full replacement for LookupFlags.Anchored

## 0.58.1.0

### Other

* Physics collision changed and body type changed events no longer get raised before initialisation

## 0.58.0.0

### Breaking changes

* Some TransformComponent functions have been moved to the system.
* Container insert, remove, and shutdown function arguments and functionality has changed.
* Physics entities without fixtures now automatically disable collision.

### New features

* Added command to profile entity spawning

### Bugfixes

* EntityLookup/BroadphaseComponent tracking has been overhauled, which should hopefully fix various broadphase bugs.

### Other

* Component.Owner is now marked as obsolete.

## 0.57.0.4

### Bugfixes

* Made entity deletion more resilient against exceptions. Should fix several bugs.

## 0.57.0.2 and 0.57.0.3

### Bugfixes

* Fixed more entity-lookup bugs.

## 0.57.0.1

### Bugfixes

* Fixed entity lookup bug that was causing crashes.

### 0.57.0.0

### Breaking changes

* EntityLookupComponent has been merged into BroadphaseComponent. The data that was previously stored in this tree is now stored across the 3 trees on BroadphaseComponent.

### New features

* EntityLookup has had its flags updated to reflect the merge of EntityLookupComponent and BroadphaseComponent, with the new flags reflecting each tree: Dynamic, Static, and Sundries. Dynamic and Static store physics bodies that are collidable and Sundries stores everything else (apart from grids).

### Internal

* EntityLookup and Broadphase have had their data de-duplicated, dropping the AABBs stored on the server by half. This also means MoveEvent updates will be much faster.
* PVS mover updates has had their performance improved slightly.
* Physics LinkedList nodes for contacts will no longer be re-made for every contact and will just be cleared when re-used.
* Sprite / Light dynamictree allocations on the client have been dropped by using static lambdas.
* The physics contact buffer for each FixtureProxy is now pooled.

## 0.56.1.1

### Bugfixes

* Fix PVS sometimes not sending an entity's parents.
* Fix velocity preservation on parenting changes.

## 0.56.1.0

### New features

* Update pt-BR locale with more localizations
* Separated PVS entity budget into an entity creation budget and a pvs-entry budget.

### Bugfixes

* Fix VV type handler removal.
* System errors during component removal should no longer result in undeletable entities.

### Other

* The ordering of component removals and shutdowns during entity deltion has changed (see #3355).
* Improved Box2Serializer
* Removed uses IEnumerables from EntityLookupSystem.
* Optimized client entity spawning by 15%.
* Modified how the rendering tree handles entity movement.
* Improved grid enumeration allocs.
* Fixed a bunch of build warnings (see #3329 and #3289 for details)

## 0.56.0.2

### Bugfixes

* Rename \_lib.ftl to \_engine_lib.ftl to avoid overwriting

## 0.56.0.1

### Bugfixes

* Fix instantiation of data records containing value types

## 0.56.0.0

### Breaking changes

* `CastShadows` moved to `SharedPointLightComponent` from clientside, now networked

### New features

* New type handler helpers added to V^3
* Added pt-BR locale

### Bugfixes

* Fixed audio fallback coords

### Other

* Improved PVS performance by using `for` over `forEach`
* Improved Vec2 inverse allocations

## 0.55.5.0

### New features

* Added a method to pass in physics transforms for getting nearest point.

### Bugfixes

* Prevent singular sprite matrices.
* Fix obsolete warnings in tests.

### Other

* Significantly reduce physics contact allocations.

## 0.55.4.1

### Breaking changes

* Removed `SI`, `SIoC`, `I`, `IoC`, `SE` and `CE` VV command prefixes.
  * `SI`, `SIoC`, `I` and `IoC` are replaced by VV paths under `/ioc/` and `/c/ioc/`.
  * `SE` and `CE` are replaced by VV paths under `/system/` and `/c/system`.

### New features

* Added CVars to control Lidgren's <abbr title="Maximum Transmission Unit">MTU</abbr> parameters:
  * `net.mtu`
  * `net.mtu_expand`
  * `net.mtu_expand_frequency`
  * `net.mtu_expand_fail_attempts`
* Added a whole load of features to ViewVariables.
  * Added VV Paths, which allow you to refer to an object by a path, e.g. `/entity/1234/Transform/WorldPosition`
  * Added VV Domains, which allow you to add "handlers" for the top-most VV Path segment, e.g. `/entity` is a domain and so is `/player`...
  * Added VV Type Handlers, which allow you to add "custom paths" under specific types, even dynamically!
  * Added VV Path networking, which allows you to read/write/invoke paths remotely, both from server to client and from client to server.
  * Added `vvread`, `vvwrite` and `vvinvoke` commands, which allow you to read, write and invoke VV paths.
  * Added autocompletion to all VV commands.
  * Please note that the VV GUI still remains the same. It will be updated to use these new features in the future.

### Other

* Changed Lidgren to be compiled against `net6.0`. This unlocks `Half` read/write methods.
* Lidgren has been updated to [0.2.2](https://github.com/space-wizards/SpaceWizards.Lidgren.Network/blob/v0.2.2/RELEASE-NOTES.md). Not all the changes since 0.1.0 are new here, since this is the first version where we're properly tracking this in release notes.
* Robust.Client now uses our own [NFluidsynth](https://github.com/space-wizards/SpaceWizards.NFluidsynth) [nuget package](https://www.nuget.org/packages/SpaceWizards.NFluidsynth).

### Internal

* Renamed Lidgren's assembly to `SpaceWizards.Lidgren.Network`.
* Rogue `obj/` folders inside Lidgren no longer break the build.
* Renamed NFluidsynth's assembly to `SpaceWizards.NFluidsynth`
