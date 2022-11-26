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
