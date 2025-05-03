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

## 239.0.1

### Bugfixes

* Fix logging of received packets with `net.packet` logging level.
* Downgrade `VorbisPizza` to fix audio playback for systems without AVX2 support.

### Other

* Improved performance of some Roslyn analyzers and source generators, which should significantly improve compile times and IDE performance.


## 239.0.0

### Breaking changes

* Robust now uses **.NET 9**.
* `ISerializationManager` will now log errors if it encounters `Entity<T>` data-fields.
  * To be clear, this has never been supported and is not really a breaking change, but this will likely require manual intervention to prevent tests from failing.
* `IClyde.TextInputSetRect`, `TextInputStart` and `TextInputStop` have been moved to be on `IClydeWindow`.
* Updated various NuGet dependencies and removed some other ones, of note:
  * `FastAccessors`, which is a transitive dep we never used, is now gone. It might have snuck into some `using` statement thanks to your IDE, and those will now fail to compile. Remove them.
  * NUnit `Is.EqualTo(default)` seems to have ambiguous overload resolution in some cases now, this can be fixed by using an explicit `default(type)` syntax.
  * This also fixed various false-positive warnings reported by NuGet.

### New features

* Added `MockInterfaces.MakeConfigurationManager` for creating functional configuration managers for unit test mocking.
* Added `ISawmill.IsLogLevelEnabled()` to avoid doing expensive verbose logging operations when not necessary.
* ``string[] Split(System.ReadOnlySpan`1<char>)`` is now available in sandbox.

### Bugfixes

* Fixed auto-generated component delta-states not raising `AfterAutoHandleStateEvent`
* Fixed auto-generated component delta-states improperly implementing `IComponentDeltaState` methods. May have caused bugs in replays.
* Fixed `Robust.Client.WebView` on the launcher via a new release.
* Fixed an exception that could occur when saving a map that had tiles migrated by alias.

### Other

* The `loglevel` command now properly shows the "`null`" log level that resets the level to inheriting from parent. This was already supported by it, but the completions didn't list it.

### Internal

* Experimental SDL2 windowing backend has been replaced with SDL3. SDL3 backend is also more feature-complete, though it is still not in use.
* Updated CEF used by Robust.Client.WebView to 131.3.5.

## 238.0.1

### Bugfixes

* Fixed source generation for auto-networked EntityUid Dictionaries missing a semicolon
* Fixed PlacementManager using the wrong coordinates when deleting entities in an area.


## 238.0.0

### Breaking changes

* Some toolshed command syntax/parsing has changed slightly, and several toolshed related classes and interfaces have changed significantly, including ToolshedManager, type parsers, invocation contexts, and parser contexts. For more detail see the the description of PR #5455


## 237.4.0

### New features

* Implement automatic field-level delta states via AutoGenerateComponentState via opt-in.

### Bugfixes

* Remove redundant TransformComponentState bool.


## 237.3.0

### New features

* Added stack-like functions to `ValueList<T>` and added an `AddRange(ReadOnlySpan<T>)` overload.
* Added new `AssetPassFilterDrop`.
* Added a new RayCastSystem with the latest Box2D raycast + shapecasts implemented.

### Bugfixes

* Fixed `IPrototypeManager.TryGetKindFrom()` not working for prototypes with automatically inferred kind names.

### Other

* Sandbox error reference locator now works with generic method calls.


## 237.2.0

### Breaking changes

* `SharedEyeSystem..SetTarget()` will now also automatically remove the old target from the session's ViewSubscriptions

### New features

* `ImmutableArray<T>` can now be serialized by `RobustSerializer`.
* `RequiresLocationAttribute`, used by `ref readonly`, is now allowed by the sandbox.
* Added `DAT-OBJ()` localization function, for the dative case in certain languages.
* Client builds for FreeBSD are now made.
* Added `FormattedMessage.TrimEnd()`.
* Added Toolshed `with` for `ProtoId<T>`.

### Bugfixes

* Fix `UniqueIndex<,>.RemoveRange()` and`UniqueIndexHkm<,>.RemoveRange()` clearing the whole set instead of just removing the specified values.
* Avoid server crashes on some weird console setups (notably Pterodactyl).
* Avoid unhandled exceptions during server shutdown getting swallowed due logging into a disposed logger.
* Fix sandbox definitions for `Regex` functions returning `MatchCollection`.
* Fix minor layout bugs with `SplitContainer` and `BoxContainer`.

### Other

* Changed how multi-window rendering presents to the screen with a new CVar `display.thread_unlock_before_swap`. This is an experiment to see if it solves some synchronization issues.
* View Variables no longer clears the window on refresh while waiting on response from server.
* `SpinBox` buttons now have a `+` prefix for the positive ones.
* Improve Toolshed type intersection mechanism

### Internal

* Warning cleanup.

## 237.1.0

### New features

* csi's auto import-system can now handle generic types.
* csi's reflection helpers (like `fld()`) handle private members up the inheritance chain.

### Bugfixes

* Fix `UniqueIndexHkm<,>` and, by extension, entity data storage memory leaking.
* Fix bugs related to UIScale on `OSWindow`s.


## 237.0.0

### Breaking changes

* `IClydeWindow.Size` is now settable, allowing window sizes to be changed after creation.

### New features

* The game server's `/update` endpoint now supports passing more information on why an update is available.
  * This information is accessible via `IWatchdogApi.RestartRequested`.
  * Information can be specified by passing a JSON object with a `Reason` code and `Message` field.
* Added an "Erase" button to the tile spawn menu.
* Added `OSWindow.Create()`, which allows OS windows to be created & initialised without immediately opening/showing them.

### Other

* Made `WatchdogApi` and some members of `IWatchdogApi` private. These symbols should never have been accessed by content.


## 236.1.0

### New features

* `RequiredMemberAttribute` and `SetsRequiredMembersAttribute` have been added to the sandbox whitelist. I.e., you can now use the `required` keyword in client/shared code.
* Added `SwitchExpressionException` to sandbox. This type gets used if you have a `switch` expression with no default case.
* Added `LineEdit.SelectAllOnFocus`.
* `GameTitle`, `WindowIconSet` and `SplashLogo` are exposed in `IGameController`. These will return said information set in game options or whatever is set in `manifest.yml`.
* `BoundUserInterface` inheritors now have access to `PlayerManager`.
* Added `MuteSounds` bool to `BaseButton`.
* The engine has a new future-proof HWID system.
  * The auth server now manages HWIDs. This avoids HWID impersonation attacks.
  * The auth server can return multiple HWIDs. They are accessible in `NetUserData.ModernHWIds`.
  * The auth server also returns a trust score factor, accessible as `NetUserData.Trust`.
  * HWID can be disabled client side (`ROBUST_AUTH_ALLOW_HWID` env var) or server side (`net.hwid` cvar).
  * The old HWID system is still in place. It is intended that content switches to placing new bans against the new HWIDs.
  * Old HWIDs no longer work if the connection is not authenticated.
* `launchauth` command now recognizes `SS14_LAUNCHER_APPDATA_NAME`.
* Added new overload to `EntityLookupSystem.GetEntitiesIntersecting`.
* Added `Control.RemoveChild(int childIndex)`.
* `build.entities_category_filter` allows filtering the entity spawn panel to a specific category.

### Bugfixes

* Fixed `SpriteView` offset calculations when scaled.

### Other

* Sprite flicks are applied immediately when started.
* More warning fixes.
* If the server gets shut down before finishing startup, the reason is now logged properly.


## 236.0.0

### Breaking changes

* Revert IsTouching only being set to true if the contact were laready touching in clientside physics prediction.
* Don't touch IsTouching if both bodies are asleep for clientside physics contacts. This change and the one above should fix a lot of clientside contact issues, particularly around repeated incorrect clientside contact events.

### New features

* Added an analyzer to detect duplicate Dependency fields.

### Bugfixes

* Auto-networked dictionaries now use `TryAdd()` to avoid duplicate key errors when a dictionary contains multiple unknown networked entities.
* Fixed `ICommonSession.Ping` always returning zero instead of the ping. Note that this will still return zero for client-side code when trying to get the ping of other players.
* Hot reload XAML files on rename to fix them potentially not being reloaded with Visual Studio.
* Fix TabContainer click detection for non-1.0 UI scales.

### Other

* Obsolete some static localization methods.
* Tried to improve PVS tolerance to exceptions occurring.


## 235.0.0

### Breaking changes

* Several different `AudioSystem` methods were incorrectly given a `[return: NotNullIfNotNull]` attribute. Content code that uses these methods needs to be updated to perform null checks.
* noSpawn is no longer obsolete and is now removed in lieu of the EntityCategory HideSpawnMenu.

### Bugfixes

* physics.maxlinvelocity is now a replicated cvar.
* Fix DistanceJoint debug drawing in physics not using the local anchors.
* Fixed filtered AudioSystem methods playing a sound for all players when given an empty filter.
* Fixed equality checks for `MarkupNode` not properly handling attributes.
* Fixed `MarkupNode` not having a `GetHashCode()` implementation.
* Fixed a PVS error that could occur when trying to delete the first entity that gets created in a round.
* Fixed the "to" and "take" toolshed commands not working as intended.
* Rich text controls within an `OutputPanel` control will now become invisible when they are out of view.

### Other

* Improve precision for Quaternion2D constructor from angles.


## 234.1.0

### New features

* SharedAudioSystem now has PlayLocal which only runs audio locally on the client.

### Bugfixes

* Fix AudioParams not being passed through on PlayGlobal methods.


## 234.0.0

### Breaking changes

* Remove a lot of obsoleted code that has been obsoleted for a while.

### New features

* Add another GetLocalEntitiesIntersecting override.

### Other

* Mark large replays as requiring Server GC.
* Obsolete some IResourceCache proxies.


## 233.1.0

### New features

* Add GetGridEntities and another GetEntitiesIntersecting overload to EntityLookupSystem.
* `MarkupNode` is now `IEquatable<MarkupNode>`. It already supported equality checks, now it implements the interface.
* Added `Entity<T>` overloads to the following `SharedMapSystem` methods: `GetTileRef`, `GetAnchoredEntities`, `TileIndicesFor`.
* Added `EntityUid`-only overloads to the following `SharedTransformSystem` methods: `AnchorEntity`, `Unanchor`.

### Bugfixes

* Fixed equality checks for `MarkupNode` not properly handling attributes.
* Fixed toolshed commands failing to generate error messages when working with array types
* Fixed `MarkupNode` not having a `GetHashCode()` implementation.

### Other

* If `EntityManager.FlushEntities()` fails to delete all entities, it will now attempt to do so a second time before throwing an exception.


## 233.0.2

### Bugfixes

* Fix exceptions in client game state handling for grids. Now they will rely upon the networked fixture data and not try to rebuild in the grid state handler.


## 233.0.1

### Bugfixes

* Fix IsHardCollidable component to EntityUid references.


## 233.0.0

### Breaking changes

* Made EntityRenamed a broadcast event & added additional args.
* Made test runs parallelizable.
* Added a debug assert that other threads aren't touching entities.

### Bugfixes

* Fix some entitylookup method transformations and add more tests.
* Fix mousehover not updating if new controls showed up under the mouse.

### Internal

* `ClientGameStateManager` now only initialises or starts entities after their parents have already been initialized. There are also some new debug asserts to try ensure that this rule isn't broken elsewhere.
* Engine version script now supports dashes.


## 232.0.0

### Breaking changes

* Obsolete method `AppearanceComponent.TryGetData` is now access-restricted to `SharedAppearanceSystem`; use `SharedAppearanceSystem.TryGetData` instead.

### New features

* Added `SharedAppearanceSystem.AppendData`, which appends non-existing `AppearanceData` from one `AppearanceComponent` to another.
* Added `AppearanceComponent.AppearanceDataInit`, which can be used to set initial `AppearanceData` entries in .yaml.

### Bugfixes

* Fix BUI interfaces not deep-copying in state handling.
* Add Robust.Xaml.csproj to the solution to fix some XAML issues.

### Other

* Serialization will now add type tags (`!type:<T>`) for necessary `NodeData` when writing (currently only for `object` nodes).

### Internal

* Added `ObjectSerializer`, which handles serialization of the generic `object` type.


## 231.1.1

### Bugfixes

* Fixed a bug where the client might not add entities to the broadphase/lookup components.
* Fixed  various toolshed commands not working, including `sort`, `sortdown` `join` (for strings), and `emplace`

### Other

* Toolshed command blocks now stop executing if previous errors were not handled / cleared.


## 231.1.0

### New features

* Network `InterfaceData` on `UserInterfaceComponent`.
* Added `System.Decimal` to sandbox.
* Added XAML hot reloading.
* Added API for content to write custom files into replay through `IReplayFileWriter`.

### Other

* Optimized `EntityLookup` and other physics systems.

### Internal

* Added more tests related to physics.


## 231.0.1

### Other

* Add better logging to failed PVS sends.


## 231.0.0

### Breaking changes

* ViewSubscriber has been moved to shared; it doesn't actually do anything on the client but makes shared code easier.

### New features

* ContactEnumreator exists to iterate the contacts of a particular entity.
* Add FixturesChangeComponent as a generic way to add and remove fixtures easily.
* PointLightComponent enabling / disabling now has an attempt event if you wish to block it on content side.
* There's an OpenScreenAt overload for screen-relative coordinates.
* SpriteSystem has methods to get an entity's position in sprite terms.
* EntityManager and ComponentFactory now have additional methods that interact with ComponentRegistry and ComponentRegistryEntry.

### Bugfixes

* Fix PrototypeFlags Add not actually working.
* Fix BUIs going BRRT opening and closing repeatedly upon prediction. The closing now gets deferred to the update loop if it's still closed at the end of prediction.


## 230.2.0

### New features

* Add ProcessNow for IRobustJob as a convenience method where you may not want to run a job in the background sometimes.
* Add Vector2i helpers to all 8 neighbouring directions.

### Other

* Remove IThreadPoolWorkItem interface from IRobustJob.


## 230.1.0

### New features

* You can now pass `bool[]` parameters to shaders.
* Added `toolshed.nearby_entities_limit` CVar.
* Fix `RichTextLabel.Text` to clear and reset the message properly in all cases.
* `scene` command has tab completion now.
* `devwindow` UI inspector property catches exceptions for read properties.
* `SplitContainer.Flip()`

### Bugfixes

* Fix tile enlargement not being applied for some EntityLookup queries.
* `LocalizedEntityCommands` are now not initialized inside `RobustUnitTest`, fixing guaranteed test failures.
* Fixed issues with broadphase init breaking replays frequently.
* Fix uploaded prototypes and resources for clients connecting to a server.

### Other

* Improved error reporting for DataField analyzer.


## 230.0.1


## 230.0.0

### New features

* Added `InterpolatedStringHandlerArgumentAttribute` to the sandbox whitelist.
* `IUserInterfaceManager.Popup()` popups now have a copy to clipboard button.

### Bugfixes

* Security fixes
* Fix exception in `TimedDespawnComponent` spawning another `TimedDespawnComponent`.
* Fixed pool memory leak in physics `SolveIsland`.


## 229.1.2

### Bugfixes

* Fixed a bug where the client might not add entities to the broadphase/lookup components.


## 229.1.1

### Bugfixes

* Fix some teleportation commands not working in singleplayer or replays

### Other

* Audio entity names now include the filepath of the audio being played if relevant for debugging.


## 229.1.0

### Bugfixes

* Fix multithreading bug in ParallelTracker that caused the game to crash randomly.
* Fixed IPv6-only hosts not working properly with built-in HTTP clients.

### Other

* Added obsoletion warning for `Control.Dispose()`. New code should not rely on it.
* Reduced the default tickrate to 30 ticks.
* Encryption of network messages is now done concurrently to avoid spending main thread time. In profiles, this added up to ~8% of main thread time on RMC-14.


## 229.0.0

### Breaking changes

* Fixes large entities causing entity spawn menu to break.
* Made PhysicsHull an internal ref struct for some PolygonShape speedup.

### New features

* Audio ticks-per-second is now capped at 30 by default and controlled via `audio.tick_rate` cvar.
* Add CreateWindow and CreateDisposableControl helpers for BUIs.
* Add OnProtoReload virtual method to BUIs that gets called on prototype reloads.
* Add RemoveData to AppearanceSystem data.


## 228.0.0

### Breaking changes

* The `Color` struct's equality methods now check for exact equality. Use `MathHelper.CloseToPercent(Color, Color)` for the previous functionality.
* Added a toolshed.nearby_limit cvar to limit the maximum range of the nearby command. Defaults to 200.

### New features

* Added command usage with types to Toolshed command help.
* Add Text property to RichTextLabel.
* Whitelist System.Net.IPEndPoint.
* Add event for mass & angular inertia changes.
* Add SpriteSystem.IsVisible for layers.
* Add TryQueueDeleteEntity that checks if the entity is already deleted / queuedeleted first.

### Bugfixes

* Clients connecting to a server now always load prototype uploads after resource uploads, fixing ordering bugs that could cause various errors.


## 227.0.0

### Breaking changes

* Add a `loop` arg to SpriteSystem.GetFrame in case you don't want to get a looping animation.
* Remove obsolete VisibileSystem methods.

### New features

* Added `LocalizedEntityCommands`, which are console commands that have the ability to take entity system dependencies.
* Added `BeginRegistrationRegion` to `IConsoleHost` to allow efficient bulk-registration of console commands.
* Added `IConsoleHost.RegisterCommand` overload that takes an `IConsoleCommand`.
* Added a `Finished` boolean to `AnimationCompletedEvent` which allows distinguishing if an animation was removed prematurely or completed naturally.
* Add GetLocalTilesIntersecting for MapSystem.
* Add an analyzer for methods that should call the base implementation and use it for EntitySystems.

### Bugfixes

* Fix loading replays if string package is compressed inside a zip.

### Other

* Tab completions containing spaces are now properly quoted, so the command will actually work properly once entered.
* Mark EntityCoordinates.Offset as Pure so it shows as warnings if the variable is unused.
* Networked events will always be processed in order even if late.


## 226.3.0

### New features

* `System.Collections.IList` and `System.Collections.ICollection` are now sandbox safe, this fixes some collection expression cases.
* The sandboxing system will now report the methods responsible for references to illegal items.


## 226.2.0

### New features

* `Control.VisibilityChanged()` virtual function.
* Add some System.Random methods for NextFloat and NextPolarVector2.

### Bugfixes

* Fixes ContainerSystem failing client-side debug asserts when an entity gets unanchored & inserted into a container on the same tick.
* Remove potential race condition on server startup from invoking ThreadPool.SetMinThreads.

### Other

* Increase default value of res.rsi_atlas_size.
* Fix internal networking logic.
* Updates of `OutputPanel` contents caused by change in UI scale are now deferred until visible. Especially important to avoid updates from debug console.
* Debug console is now limited to only keep `con.max_entries` entries.
* Non-existent resources are cached by `IResourceCache.TryGetResource`. This avoids the game constantly trying to re-load non-existent resources in common patterns such as UI theme texture fallbacks.
* Default IPv4 MTU has been lowered to 700.
* Update Robust.LoaderApi.

### Internal

* Split out PVS serialization from compression and sending game states.
* Turn broadphase contacts into an IParallelRobustJob and remove unnecessary GetMapEntityIds for every contact.


## 226.1.0

### New features

* Add some GetLocalEntitiesIntersecting methods for `Entity<T>`.

### Other

* Fix internal networking logic


## 226.0.0

### Breaking changes

* `IEventBus.RaiseComponentEvent` now requires an EntityUid argument.
* The `AddedComponentEventArgs` and `RemovedComponentEventArgs` constructors are now internal

### New features

* Allow RequestScreenTexture to be set in overlays.

### Bugfixes

* Fix AnimationCompletedEvent not always going out.


## 225.0.0

### Breaking changes

* `NetEntity.Parse` and `TryParse` will now fail to parse empty strings.
* Try to prevent EventBus looping. This also caps the amount of directed component subscriptions for a particular component to 256.

### New features

* `IPrototypeManager.TryIndex` will now default to logging errors if passed an invalid prototype id struct (i,e., `EntProtoId` or `ProtoId<T>`). There is a new optional bool argument to disable logging errors.
* `Eye` now allows its `Position` to be set directly. Please only do this with the `FixedEye` child type constructed manually.
* Engine now respects the hub's `can_skip_build` parameter on info query, fixing an issue where the first hub advertisement fails due to ACZ taking too long.
* Add GetSession & TryGetSession to ActorSystem.
* Raise an event when an entity's name is changed.

### Bugfixes

* The `ent` toolshed command now takes `NetEntity` values, fixing parsing in practical uses.
* Fix ComponentFactory test mocks.
* Fix LookupFlags missing from a couple of EntityLookupSystem methods.

### Other

* Improved engine's Happy Eyeballs implementation, should result in more usage of IPv6 for HTTP APIs when available.
* Remove CompIdx locks to improve performance inside Pvs at higher player counts.
* Avoid a read lock in GetEntityQuery to also improve performance.
* Mark `EntityManager.System<T>` as Pure.


## 224.1.1

### Bugfixes

* Fixed UserInterfaceSystem sometimes throwing a key-not-found exception when trying to close UIs.


## 224.1.0

### New features

* `ServerIntegrationInstance` has new methods for adding dummy player sessions for tests that require multiple players.
* Linguini has been updated to v0.8.1. Errors will now be logged when a duplicate localization key is found.
* Added `UserInterfaceSystem.SetUi()` for modifying the `InterfaceData` associated with some BUI.
* Added the `EntityPrototypeView` control for spawning & rendering an entity prototype.

### Bugfixes

* Fix `UserInterfaceSystem` spamming client side errors when entities with UIs open are deleted while outside of PVS range.
* Fix Toolshed's EnumTypeParse not working enum values with upercase characters.
* Fixed `incmd` command not working due to an invalid cast.

### Other

* There have been various performance improvements to replay loading & playback.

### Internal

* Added `DummySession` and `DummyChannel` classes for use in integration tests and benchmarks to fool the server into thinking that there are multiple players connected.
* Added `ICommonSessionInternal` and updated `CommonSession` so that the internal setters now go through that interface.

## 224.0.1

### Bugfixes

* Fixes PVS throwing exceptions when invalid entities are passed to `ExpandPvsEvent`. Now it just logs an error.
* Fixes BUIs not properly closing, resulting in invalid entities in `UserInterfaceUserComponent.OpenInterfaces`
* Fixes an unknown/invalid prototype exception sometimes being thrown when running ``IPrototypeManager.ResolveResults()`


## 224.0.0

### Breaking changes

* `Matrix3` has been replaced with `System.Numerics.Matrix3x2`. Various Matrix related methods have been turned into extension methods in the `Matrix3Helpers` class.
* Engine `EntityCategory` prototype IDs have been changed to use CamelCase. I.e., `hideSpawnMenu` -> `HideSpawnMenu`
* Prototypes can now be implicitly cast `ProtoId<T>` or `EntProtoId` ID structs. The new implicit cast might cause previous function calls to be ambiguous.

### New features

* `Array.Clear(Array)` is now available in the sandbox.
* BUIs now use `ExpandPvsEvent`. I.e., if a player has a UI open, then the entity associated with that UI will always get sent to the player by the PVS system.
* Added `cvar_subs` command for listing all subscribers to cvar changes
* Entity categories have been reworked
  * Each category now has a `HideSpawnMenu` field. The old `HideSpawnMenu` category is now just a normal category with that field set to true.
  * Reworked category inheritance. Inheritance can now be disabled per category using a `Inheritable` field.
  * Entity prototypes can now be automatically added to categories based on the components that they have, either by specifying components when defining the category in yml, or by adding the EntityCategoryAttribute to the component class.

### Bugfixes

* Fixed client-side BUI error log spam if an unknown entity has a UI open.
* Fixed placement manager spawning entities with incorrect rotations.

### Other

* Added a try-catch block to BUI constructors, to avoid clients getting stuck in error loops while applying states.
* Attempting to play sounds on terminating entities no longer logs an error.


## 223.3.0

### New features

* Better exception logging for IRobustJob.
* Add SetGridAudio helper for SharedAudioSystem.

### Bugfixes

* Fix placement manager not setting entity rotation correctly.
* Fix grid-based audio not playing correctly.


## 223.2.0

### New features

* Added several new `FormattedMessage` methods for better exception tolerance when parsing markup. Several existing methods have been marked as obsolete, with new renamed methods taking their place.


## 223.1.2

### Bugfixes

* `MapGridComponent.LastTileModifiedTick` is now actually networked to clients.


## 223.1.1

### Bugfixes

* Fixed an exception caused by enum cvars using integer type values instead of enum values


## 223.1.0

### Other

* Various `ContainerSystem` methods have been obsoleted in favour of overrides that take in an `Entity` struct instead of `EntityUid`
* Various `EntityCoordinates` methods have been obsoleted with replacements added  to `SharedTransformSystem`


## 223.0.0

### Breaking changes

* The `ComponentState` class is now abstract. Networked components that don't have state information now just return a null state.
* The way that delta component states work has changed. It now expects there to be two different state classes, only one of which should implement `IComponentDeltaState<TFullState>`

### New features

* A new `replay.checkpoint_min_interval` cvar has been added. It can be used to limit the frequency at which checkpoints are generated when loading a replay.
* Added `IConfigurationManager.OnCVarValueChanged`. This is a c# event that gets invoked whenever any cvar value changes.

### Bugfixes

* `IEyeManager.GetWorldViewbounds()` and `IEyeManager.GetWorldViewbounds()` should now return the correct bounds if the main viewport does not take up the whole screen.

### Other

* The default values of various replay related cvars have been changed to try and reduce memory usage.


## 222.4.0

### New features

* Added the following types from `System.Numerics` to the sandbox: `Complex`, `Matrix3x2`, `Matrix4x4`, `Plane`, `Quaternion`, `Vector3`, `Vector4`.


## 222.3.0

### New features

* `ITileDefinition.EditorHidden` allows hiding a tile from the tile spawn panel.
* Ordered event subscriptions now take child types into account, so ordering based on a shared type will work.

### Bugfixes

* Cross-map BUI range checks now work.
* Paused entities update on prototype reload.

### Other

* Fixed build compatibility with .NET 8.0.300 SDK, due to changes in how Central Package Management behaves.
* Physics component has delta states to reduce network usage.


## 222.2.0

### New features

* Added `EntityQuery.Comp()` (abbreviation of `GetComponent()`)

### Bugfixes

* Fix `SerializationManager.TryGetVariableType` checking the wrong property.
* Fixed GrammarSystem mispredicting a character's gender

### Other

* User interface system now performs range checks in parallel


## 222.1.1

### Bugfixes

* Fixed never setting BoundUserInterface.State.

### Other

* Add truncate for filesaving.
* Add method for getting the type of a data field by name from ISerializationManager.


## 222.1.0

### New features

* Added `BoundKeyEventArgs.IsRepeat`.
* Added `net.lidgren_log_warning` and `net.lidgren_log_error` CVars.

### Bugfixes

* Fix assert trip when holding repeatable keybinds.

### Other

* Updated Lidgren to v0.3.1. This should provide performance improvements if warning/error logs are disabled.


## 222.0.0

### Breaking changes

* Mark IComponentFactory argument in EntityPrototype as mandatory.

### New features

* Add `EntProtoId<T>` to check for components on the attached entity as well.

### Bugfixes

* Fix PVS iterating duplicate chunks for multiple viewsubscriptions.

### Other

* Defer clientside BUI opens if it's the first state that comes in.


## 221.2.0

### New features

* Add SetMapAudio helper to SharedAudioSystem to setup map-wide audio entities.
* Add SetWorldRotNoLerp method to SharedTransformSystem to avoid client lerping.

### Bugfixes

* `SpriteComponent.CopyFrom` now copies `CopyToShaderParameters` configuration.


## 221.1.0


## 221.0.0

### Breaking changes

* `EntParentChangedMessage.OldMapId` is now an `EntityUid` instead of `MapId`
* `TransformSystem.DetachParentToNull()` is being renamed to `DetachEntity`
* The order in which `MoveEvent` handlers are invoked has been changed to prioritise engine subscriptions

### New features

* Added `UpdateHovered()` and `SetHovered()` to `IUserInterfaceManager`, for updating or modifying the currently hovered control.
* Add SwapPositions to TransformSystem to swap two entity's transforms.

### Bugfixes

* Improve client gamestate exception tolerance.

### Other

* If the currently hovered control is disposed, `UserInterfaceManager` will now look for a new control, rather than just setting the hovered control to null.

### Internal

* Use more `EntityQuery<T>` internally in EntityManager and PhysicsSystem.


## 220.2.0

### New features

* RSIs can now specify load parameters, mimicking the ones from `.png.yml`. Currently only disabling sRGB is supported.
* Added a second UV channel to Clyde's vertex format. On regular batched sprite draws, this goes 0 -> 1 across the sprite quad.
* Added a new `CopyToShaderParameters` system for `SpriteComponent` layers.


## 220.1.0

### Bugfixes

* Fix client-side replay exceptions due to dropped states when recording.

### Other

* Remove IP + HWId from ViewVariables.
* Close BUIs upon disconnect.


## 220.0.0

### Breaking changes

* Refactor UserInterfaceSystem.
  - The API has been significantly cleaned up and standardised, most noticeably callers don't need to worry about TryGetUi and can rely on either HasUi, SetUiState, CloseUi, or OpenUi to handle their code as appropriate.
  - Interface data is now stored via key rather than as a flat list which is a breaking change for YAML.
  - BoundUserInterfaces can now be completely handled via Shared code. Existing Server-side callers will behave similarly to before.
  - BoundUserInterfaces now properly close in many more situations, additionally they are now attached to the entity so reconnecting can re-open them and they can be serialized properly.


## 219.2.0

### New features

* Add SetMapCoordinates to TransformSystem.
* Improve YAML Linter and validation of static fields.

### Bugfixes

* Fix DebugCoordsPanel freezing when hovering a control.

### Other

* Optimise physics networking to not dirty every tick of movement.


## 219.1.3

### Bugfixes

* Fix map-loader not pausing pre-init maps when not actively overwriting an existing map.


## 219.1.2

### Bugfixes

* Fix map-loader not map-initialising grids when loading into a post-init map.


## 219.1.1

### Bugfixes

* Fix map-loader not map-initialising maps when overwriting a post-init map.


## 219.1.0

### New features

* Added a new optional arguments to various entity spawning methods, including a new argument to set the entity's rotation.

### Bugfixes

* Fixes map initialisation not always initialising all entities on a map.

### Other

* The default value of the `auth.mode` cvar has changed


## 219.0.0

### Breaking changes

* Move most IMapManager functionality to SharedMapSystem.


## 218.2.0

### New features

* Control layout properties such as `Margin` can now be set via style sheets.
* Expose worldposition in SpriteComponent.Render
* Network audio entity Play/Pause/Stop states and playback position.
* Add `Disabled` functionality to `Slider` control.


## 218.1.0

### New features

* Add IEquatable.Equals to the sandbox.
* Enable roslyn extensions tests in CI.
* Add a VerticalTabContainer control to match the horizontal one.

### Bugfixes

* Fix divison remainder issue for Colors, fixing purples.

### Other

* Default hub address (`hub.hub_urls`) has been changed to `https://hub.spacestation14.com/`.


## 218.0.0

### Breaking changes

* `Robust.Shared.Configuration.EnvironmentVariables` is now internal and no longer usable by content.

### New features

* Add TryGetRandom to EntityManager to get a random entity with the specified component and TryGetRandom to IPrototypeManager to return a random prototype of the specified type.
* Add CopyData to AppearanceSystem.
* Update UI themes on prototype reloads.
* Allow scaling the line height of a RichTextLabel.
* You can now specify CVar overrides via environment variable with the `ROBUST_CVAR_*` prefix. For example `ROBUST_CVAR_game__hostname=foobar` would set the appropriate CVar. Double underscores in the environment variable name are replaced with ".".
* Added non-generic variant of `GetCVar` to `IConfigurationManager`.
* Add type tracking to FieldNotFoundErrorNode for serialization.
* Distance between lines of a `RichTextLabel` can now be modified with `LineHeightScale`.
* UI theme prototypes are now updated when reloaded.
* New `RA0025` analyzer diagnostic warns for manual assignment to `[Dependency]` fields.

### Bugfixes

* Request headers in `IStatusHandlerContext` are now case-insensitive.
* SetWorldPosition rotation now more closely aligns with prior behavior.
* Fix exception when inspecting elements in some cases.
* Fix HTTP errors on watchdog ping not being reported.

### Other

* Add an analyzer for redundantly assigning to dependency fields.

### Internal

* Remove redundant Exists checks in ContainerSystem.
* Improve logging on watchdog pings.


## 217.2.1

### Bugfixes

* Fix LineEdit tests on engine.

### Internal

* Make various ValueList enumerators access the span directly for performance.


## 217.2.0

### New features

* Added `AddComponents` and `RemoveComponents` methods to EntityManager that handle EntityPrototype / ComponentRegistry bulk component changes.
* Add double-clicking to LineEdit.

### Bugfixes

* Properly ignore non-hard fixtures for IntersectRayWithPredicate.
* Fix nullable TimeSpan addition on some platforms.


## 217.1.0

### New features

* Added `IRobustRandom.GetItems` extension methods for randomly picking multiple items from a collections.
* Added `SharedPhysicsSystem.EffectiveCurTime`. This is effectively a variation of `IGameTiming.CurTime` that takes into account the current physics sub-step.

### Bugfixes

* Fix `MapComponent.LightingEnabled` not leaving FOV rendering in a broken state.

### Internal

* `Shuffle<T>(Span<T>, System.Random)` has been removed, just use the builtin method.


## 217.0.0

### Breaking changes

* TransformSystem.SetWorldPosition and SetWorldPositionRotation will now also perform parent updates as necessary. Previously it would just set the entity's LocalPosition which may break if they were inside of a container. Now they will be removed from their container and TryFindGridAt will run to correctly parent them to the new position. If the old functionality is desired then you can use GetInvWorldMatrix to update the LocalPosition (bearing in mind containers may prevent this).

### New features

* Implement VV for AudioParams on SoundSpecifiers.
* Add AddUi to the shared UI system.

### Bugfixes

* Fix the first measure of ScrollContainer bars.


## 216.0.0

### Breaking changes

* The `net.low_lod_distance` cvar has been replaced with a new `net.pvs_priority_range`. Instead of limiting the range at which all entities are sent to a player, it now extends the range at which high priorities can be sent. The default value of this new cvar is 32.5, which is larger than the default `net.pvs_range` value of 25.

### New features

* You can now specify a component to not be saved to map files with `[UnsavedComponent]`.
* Added `ITileDefinitionManager.TryGetDefinition`.
* The map loader now tries to preserve the `tilemap` contents of map files, which should reduce diffs when re-saving a map after the game's internal tile IDs have changed.

### Bugfixes

* Fix buffered audio sources not being disposed.


## 215.3.1

### Bugfixes

* Revert zstd update.


## 215.3.0

### New features

* `EntityQuery<T>` now has `HasComp` and `TryComp` methods that are shorter than its existing ones.
* Added `PlacementInformation.UseEditorContext`.
* Added `Vector2Helpers` functions for comparing ranges between vectors.

### Bugfixes

* `Texture.GetPixel()`: fixed off-by-one with Y coordinate.
* `Texture.GetPixel()`: fix stack overflow when reading large images.
* `Texture.GetPixel()`: use more widely compatible OpenGL calls.

### Other

* Disabled `net.mtu_expand` again by default, as it was causing issues.
* Updated `SharpZstd` dependency.


## 215.2.0

### New features

* Implement basic VV for SoundSpecifiers.

### Bugfixes

* Fix QueueDel during EndCollideEvents from throwing while removing contacts.


## 215.1.0

### New features

* Add a CompletionHelper for audio filepaths that handles server packaging.
* Add Random.NextAngle(min, max) method and Pick for `ValueList<T>`.
* Added an `ICommonSession` parser for toolshed commands.

### Bugfixes


## 215.0.0

### Breaking changes

* Update Lidgren to 0.3.0

### New features

* Made a new `IMetricsManager` interface with an `UpdateMetrics` event that can be used to update Prometheus metrics whenever they are scraped.
  * Also added a `metrics.update_interval` CVar to go along with this, when metrics are scraped without usage of Prometheus directly.
* IoC now contains an `IMeterFactory` implementation that you can use to instantiate metric meters.
* `net.mtu_ipv6` CVar allows specifying a different MTU value for IPv6.
* Allows `player:entity` to take a parameter representing the player name.
* Add collection parsing to the dev window for UI.
* Add a debug assert to Dirty(uid, comp) to catch mismatches being passed in.

### Bugfixes

* Support transform states with unknown parents.
* Fix serialization error logging.
* Fix naming of ResizableMemoryRegion metrics.
* Fix uncaught overflow exception when parsing NetEntities.

### Other

* The replay system now allows loading a replay with a mismatching serializer type hash. This means replays should be more robust against future version updates (engine security patches or .NET updates).
* `CheckBox`'s interior texture is now vertically centered.
* Lidgren.Network has been updated to [`v0.3.0`](https://github.com/space-wizards/SpaceWizards.Lidgren.Network/blob/v0.3.0/RELEASE-NOTES.md).
* Lowered default IPv4 MTU to 900 (from 1000).
* Automatic MTU expansion (`net.mtu_expand`) is now enabled by default.

### Internal

* Cleanup some Dirty component calls internally.


## 214.2.0

### New features

* Added a `Undetachable` entity metadata flag, which stops the client from moving an entity to nullspace when it moves out of PVS range.

### Bugfixes

* Fix tooltips not clamping to the left side of the viewport.
* Fix global audio property not being properly set.

### Internal

* The server game state / PVS code has been rewritten. It should be somewhat faster now, albeit at the cost of using more memory. The current engine version may be unstable.


## 214.1.1

### Bugfixes

* Fixed connection denial always causing redial.


## 214.1.0

### New features

* Added the `pvs_override_info` command for debugging PVS overrides.

### Bugfixes

* Fix VV for prototype structs.
* Fix audio limits for clientside audio.


## 214.0.0

### Breaking changes

* `NetStructuredDisconnectMessages` has received a complete overhaul and has been moved to `NetDisconnectMessage`. The API is no longer designed such that consumers must pass around JSON nodes, as they are not in sandbox (and clunky).

### New features

* Add a basic default concurrent audio limit of 16 for a single filepath to avoid overflowing audio sources.
* `NetConnectingArgs.Deny()` can now pass along structured data that will be received by the client.

### Bugfixes

* Fixed cursor position bugs when an empty `TextEdit` has a multi-line place holder.
* Fixed empty `TextEdit` throwing exception if cursor is moved left.


## 213.0.0

### Breaking changes

* Remove obsoleted BaseContainer methods.

### New features

* Add EntityManager.RaiseSharedEvent where the event won't go to the attached client but will be predicted locally on their end.
* Add GetEntitiesInRange override that takes in EntityCoordinates and an EntityUid hashset.

### Bugfixes

* Check if a sprite entity is deleted before drawing in SpriteView.


## 212.2.0

### New features

* Add IsHardCollidable to SharedPhysicsSystem to determine if 2 entities would collide.

### Other

* Double the default maximum replay size.


## 212.1.0

### New features

* Add nullable methods for TryIndex / HasIndex on IPrototypeManager.

### Bugfixes

* Fix TextureRect alignment where the strech mode is KeepCentered.


## 212.0.1

### Bugfixes

* Fix passing array by `this` instead of by `ref`.


## 212.0.0

### Breaking changes

* Change Collapsible controls default orientations to Vertical.

### New features

* Expose the Label control for Collapsible controls.
* Add GetGridPosition that considers physics center-of-mass.
* Add TileToVector methods to get the LocalPosition of tile-coords (taking into account tile size).
* Add some more helper methods to PVS filters around EntityUids.
* Add support for Dictionary AutoNetworkedFields.
* Add EnsureLength method for arrays.
* Add PushMarkup to FormattedMessage.
* Add DrawPrimitives overload for `List<Vector2>`
* Add more ValueList ctors that are faster.
* Add ToMapCoordinates method for NetCoordinates.

### Other

* Remove ISerializationHooks obsoletion as they are useful in some rare cases.

### Internal

* Bump max pool size for robust jobs.


## 211.0.2

### Bugfixes

* Fix TextureRect scaling not handling UIScale correctly.


## 211.0.1

### Bugfixes

* Fix GridChunkEnumerator on maps.


## 211.0.0

### Breaking changes

* Moved ChunkIndicesEnumerator to engine and to a re-useable namespace at Robust.Shared/Maps.

### New features

* Added an Enlarged method for Box2Rotated.

### Internal

* Significantly optimise ChunkEnumerator / FindGridsIntersecting in certain use cases by intersecting the grid's AABB with the local AABB to avoid iterating dummy chunks.


## 210.1.1

### Bugfixes

* Fixed multiple recent bugs with key binding storage.

### Other

* Change default of `ButtonGroup.IsNoneSetAllowed` to `true`. This makes it default again to the previous (unintentional) behavior.


## 210.1.0

### New features

* `NetUserId` implements `ISelfSerialize` so can be used in data fields.
* `ButtonGroup.IsNoneSetAllowed` to allow a button group to have no buttons pressed by default.


## 210.0.3


## 210.0.2

### Bugfixes

* Revert changes to `TextureRect` too.


## 210.0.1

### Bugfixes

* Revert changes to `TextureButton` that broke style property handling.


## 210.0.0

### New features

* Controls can now hook before, after, and during rendering of their children.
* IRenderHandle is now a public API, with the caveat that it's properties and methods are unstable.
* ButtonGroup now exposes what buttons it contains, alongside which is currently pressed.
* OptionButton has additional styleclasses, and has a hook for modifying it's internal buttons.
* PanelContainer.GetStyleBox() is now protected rather than private.
* TextureButton now uses a TextureRect instead of custom drawing code.
* TextureRect has additional style properties exposed.
    * A new property, TextureSizeTarget, was added, which allows specifying a size in virtual pixels that the control should attempt to draw at.
    * Stretch mode is now a style property.
    * Scale is now a style property.
* Avalonia.Metadata.XmlnsDefinitionAttribute is now permitted by the sandbox.
* Add MaxDimension property to Box2 to return the higher of the Width or Height.
* Add GetLocalPosition to convert ScreenCoordinates to coordinates relative to the control. Ignores window.
* Add GlobalRect and GlobalPixelRect for controls to get their UIBox2i in screen terms.
* Add dotted line drawing to DrawingHandleScreen.
* You can use `Subs.CVar()` from an entity systems to subscribe to CVar changes. This is more convenient than `IConfigurationManager.OnValueChanged` as it automatically unsubscribes on system shutdown.
* There is now a built-in type serializer for `DateTime`, so you can put `DateTime`s in your data fields.
* `System.Text.Unicode.UnicodeRange` and `UnicodeRanges` are now available in the sandbox.

### Bugfixes

* UI drawing now properly accounts for a control's draw routine potentially mangling the current matrix.
* UI roots now properly update when the global stylesheet is changed. They previously only did so if they had a dedicated stylesheet (which is the one case where they would be unaffected by a global sheet update.


## 209.0.1

### Bugfixes

* Fix missed import from 209.0.0.


## 209.0.0

### Breaking changes

* `replay.max_compressed_size` and `replay.max_uncompressed_size` CVars are now `long`.
* Remove obsolete CoordinatesExtension for ToEntityCoordinates from GridUid / Vector2i.

### New features

* Add GetEntitiesOnMap / GetChildEntities to EntityLookupSystem to return components on the specified map and components with the specified parent respectively.
* Add MaxDimension property to Box2 to return the higher of the Width or Height.
* Add GetLocalPosition to convert ScreenCoordinates to coordinates relative to the control. Ignores window.
* Add GlobalRect and GlobalPixelRect for controls to get their UIBox2i in screen terms.
* Add dotted line drawing to DrawingHandleScreen.
* `IConfigurationManager.LoadDefaultsFromTomlStream` properly does type conversions. This fixes scenarios like loading of `long` CVars.
* Add helper methods for TileRef / Vector2i to SharedMapSystem for ToCenterCoordinates (tile center EntityCoordinates) and ToCoordinates (tile origin to EntityCoordinates).
* Copy some of the coordinates extensions to SharedTransformSystem.

### Bugfixes

* Fixed integer overflows in replay max size calculation.
* Explicitly capped `replay.replay_tick_batchSize` internally to avoid high values causing allocation failures.

### Other

* Important MIDI performance improvements.


## 208.0.0

### Breaking changes

* Metadata flags are no longer serialized as they get rebuilt on entity startup.

### Bugfixes

* Log failing to load user keybinds and handle the exception.


## 207.1.0

### New features

* Add the ability to merge grids via GridFixtureSystem.


## 207.0.0

### Breaking changes

* Update EntityLookup internally so non-approximate queries use the GJK solver and are much more accurate. This also means the approximate flag matters much more if you don't need narrowphase checks.
* Add shape versions of queries for both EntityLookup and MapManager.

### Bugfixes

* Fix PVS full state updates not clearing session entities and causing exceptions.

### Other

* Integration tests now run `NetMessage`s through serialization rather than passing the objects between client and server. This causes tests that missed `[NetSerializer]` attributes on any objects that need them to fail.

### Internal

* Remove a lot of duplicate code internally from EntityLookup and MapManager.


## 206.0.0

### Breaking changes

* tpto will teleport you to physics-center instead of transform center instead.
* Rename local EntityLookup methods to reflect they take local AABBs and not world AABBs.

### New features

* Add some additional EntityLookup methods for local queries.
* Add support to PrototypeManager for parsing specific files / directories as abstract.

### Bugfixes

* Fix tpto short-circuiting if one of the listed entities isn't found.
* Fix tpto not allowing grids as targets.

### Other

* Reduce MIDI source update rate from 10hz to 4hz.

### Internal

* Remove some duplicate internal code in EntityLookupSystem.
* Skip serialization sourcegen in GLFW and Lidgren.


## 205.0.0

### Breaking changes

* The unused `Robust.Physics` project has been deleted.
* The project now uses [Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management).
* (Almost) all the NuGet packages have been updated. This causes many problems. I am so sorry.
* Cleaned up some unused packages as well.


## 204.1.0

### New features

* New `EntitySystem` subscription helper for working with Bound User Interface events. You can find them by doing `Subs.BuiEvents<>()` in a system.
* The `EntityManager.Subscriptions` type (for building helper extension methods) now uses

### Bugfixes

* Avoid loading assemblies from content `/Assemblies` if Robust ships its own copy. This avoid duplicate or weird mismatching version issues.

### Other

* Removed glibc version check warning.


## 204.0.0

### Breaking changes

* Make EntityManager abstract and make IEntityManager.EntityNetManager not nullable.
* Make VVAccess.ReadWrite default for all Datafields instead of VVAccess.ReadOnly

### New features

* `TextEdit.OnTextChanged`
* Add Pick and PickAndTake versions for System.Random for ICollections.

### Bugfixes

* Fix `IClipboardManager.GetText()` returning null in some cases.
* Fix possible NRE in server-side console command completion code.
* Fix possible NRE on DebugConsole logs.
* Fix exception when VVing non-networked components.

### Other

* Remove "Do not use from content" from IComponent.


## 203.0.0

### Breaking changes

* `IComponentFactory.RegisterIgnore()` no longer supports overwriting existing registrations, components should get ignored before they are registered.
* Event bus subscriptions are now locked after `IEntityManager` has started, instead of after the first component gets added. Any event subscriptions now need to happen before startup (but after init).
* Event bus subscriptions must now be locked before raising any events.
* Delete FodyWeavers.xsd as it hasn't been used for a long time.
* Remove physics sleep cancelling as it was, in hindsight, a bad idea.

### New features

* `RobustUnitTest` now has a `ExtraComponents` field for automatically registering additional components.
* `IComponentFactory.RegisterIgnore()` now accepts more than one string.
* Added `IComponentFactory.RegisterTypes` for simultaneously registering multiple components.

### Bugfixes

* Clamp volume calculations for audio rather than throwing.


## 202.1.1

### Bugfixes

* Reverted some map/grid initialisation changes that might've been causing broadphase/physics errors.
* Fixed PVS sometimes sending entities without first sending their children.
* Fixed a container state handling bug caused by containers not removing expected entities when shutting down.
* Fixed a `EnsureEntity<T>` state handling bug caused by improper handling of entity deletions.
* Fixed a bad NetSyncEnabled debug assert.


## 202.1.0

### New features

* Add GetLocalEntitiesIntersecting overload that takes in a griduid and a Vector2i tile.


## 202.0.0

### Breaking changes

* Various entity manager methods now have a new `where T : IComponent` constraint.
* The `IComponentFactory.ComponentAdded` event has been renamed to `ComponentsAdded` and now provides an array of component registrations.
* `IComponentFactory.RegisterIgnore()` no longer supports overwriting existing registrations, components should get ignored before they are registered.

### New features

* Added `IComponentFactory.GetAllRegistrations()`
* Add IComponentState interface support for component states so structs can be used in lieu of classes.


## 201.0.0

### Breaking changes

* The `zCircleGradient` shader function arguments have changed. It now requires a pixel-size to ensure that the gradient is properly entered.

### Bugfixes

* Fixed some PVS null reference errors.


## 200.0.0

### Breaking changes

* MappingDataNode is now ordered.
* Make invalid AutoNetworkedFields compiler errors.

### New features

* `OSWindowStyles.NoTitleBar` (supported only on Linux X11 for now).

### Bugfixes

* Avoid calling DirtyEntity when a component's last modified tick is not current.
* Fix `tpgrid` allowing moving grids to nullspace.

### Other

* `OSWindowStyles.NoTitleOptions` is now supported on Linux X11.


## 199.0.0

### Breaking changes

* Various `IEntityManager` C# events now use `Entity<MetadataComponent>` instead of `EntityUid`
* Entity visibility masks now use a ushort instead of an integer.
* Run grid traversal on entity spawn.

### New features

* Added two new `IEntityManager` C# events that get raiseed before and after deleting ("flushing") all entities.
* Added a new `DeleteEntity()` override that takes in the entity's metadata and transform components.
* Add better audio logs.
* Expand z-library shader.
* Add a Box2i union for Vector2i and add a Contains variant that assumes the Vector2i is a tile and not a point.

### Bugfixes

* Try to prevent some NREs in PVS.
* More PVS fixes and cleanup.


## 198.1.0

### New features

* `IClydeViewport` now provides access to the light render target.
* Added a style-class to the `MenuBar` popup control.
* Added `NextGaussian()` extension method for `System.Random`.
* Added per-session variant of `PvsOverrideSystem.AddForceSend()`.

### Bugfixes

* Stopped the client from logging errors when attempting to delete invalid entities.

### Other

* The `DevWindow` UI inspector has been improved a bit and it now groups properties by their defining type.


## 198.0.1

### Bugfixes

* Fix preprocessor flag for FULL_RELEASE preventing building.


## 198.0.0

### Breaking changes

* Disable DefaultMagicAczProvider for FULL_RELEASE as it's only meant for debugging.

### New features

* Automatic UI scale is disabled by default for non-main windows. If desired, it can be re-enabled per window by changing `WindowRoot.DisableAutoScaling`.
* Add UI click and hover sound support via IUserInterfaceManager.SetClickSound / .SetHoverSound

### Bugfixes

* Fix GetEntitiesIntersecting for map entities without grids.

### Other

* Print more diagnostics on server startup.


## 197.1.0

### New features

* ACZ improvements: `IStatusHost.InvalidateAcz()` and `IStatusHost.SetFullHybridAczProvider()`.

### Bugfixes

* Fixes a PVS bug that happens when grids moved across maps.
* Fixes sprite animations not working properly


## 197.0.0

### Breaking changes

* PvsOverrideSystem has been reworked:
  * Session and global overrides now default to always being recursive (i.e., sending all children).
  * Session & global overrides will always respect a client's PVS budgets.
  * Entities with an override will now still be sent in the same way as other entities if they are within a player's view. If you want to prevent them from being sent, you need to use visibility masks.
  * Entities can have more than one kind of override (i.e., multiple sessions).

### New features

* Added a `PvsSize ` field to `EyeComponent`, which can be used to modify the PVS range of an eye.
* Added a new `NetLowLodRange` cvar for reducing the number of distant entities that get sent to a player. If a PVS chunk is beyond this range (but still within PVS range), then only high-priority entities on that chunk will get sent.
* Added a new metadata flag for tagging an entity as a "high prority" entity that should get sent even on distant chunks. This only works for entities that are directly attached to a grid or map. This is currently used by lights & occluders.

### Other

* PVS has been reworked again, and should hopefully be noticeable faster.
* PVS now prioritizes sending chunks that are closer to a player's eyes.


## 196.0.0

### Breaking changes

* Dirtying a non-networked component will now fail a debug assert.
* The `IInvocationContext` interface for toolshed commands now requires a UserId field. The session field should be cleared if a player disconnects.

### New features

* `LocalizationManager` now supports multiple fallback cultures
* SpriteView now supports using a `NetEntity` to select an entity to draw.
* Added methods for simultaneously dirtying several components on the same entity.
* Animated sprite layers now have a "Cycle" option that will reverse an animation when it finishes.

### Bugfixes

* Fixed a recursion/stack-overflow in `GridTraversalSystem`
* Ensure `Robust.Client.WebView` processes get shut down if game process exits uncleanly.
* Fixed Toolshed commands not properly functioning after disconnecting and reconnecting.

### Other

* Console command completions no longer suggest toolshed commands for non-toolshed commands.



## 195.0.1

### Bugfixes

* Fixes playing audio using audio streams
* Fixes placement manager exceptions when placing self deleting / spawner entities
* Fixed `IPrototypeManager.EnumeratePrototypes<T>` throwing an exception when there are no instances.


## 195.0.0

### New features

* Generic versions of `DebugTools.AssertEquals()` functions.
* `[Prototype]` now does not need to have a name specified, the name is inferred from the class name.

### Bugfixes

* Fixes a physics bug that could cause deleted entities to remain on the physics map.
* Fixes a bug in entity lookup code that could cause clients to get stuck in an infinite loop.

### Other

* `Robust.Client.WebView` has been brought alive again.
* The addition of physics joints is no longer deferred to the next tick.
* Grid traversal is no longer deferred to the next tick.
* Integration tests now fail when console commands log errors.


## 194.1.0

### New features

* `IAudioManager` has APIs to directly load `AudioStream`s from data streams.
* `AudioSystem` has new `Play*` methods.
* `EntityCoordinates.TryDelta()`
* `EntityLookupSystem.GetEntitiesInRange()` untyped hashset overload has `flags` parameter.


## 194.0.2

### Internal

* Added some null-checks to PVS to try reduce the error spam.


## 194.0.1

### Bugfixes

* Fixed `Control.SetPositionInParent` failing to move an entity to the last position.
* Fixed audio occlusion not working.

### Internal

* Added some logs for grid/map deletion and movement to debug some map loading issues.
* Refactored some parts of PVS. It should be slightly faster, though the game may be unstable for a bit.

## 194.0.0

### Breaking changes

* MoveEvent is no longer raised broadcast, subscribe to the SharedTransformSystem.OnGlobalMoveEvent C# event instead

### Bugfixes

* Fixed the game sometimes freezing while trying to load specific audio files.


## 193.2.0

### Other

* Added more PVS error logs


## 193.1.1

### Bugfixes

* Fixed an exception when building in FULL_RELEASE


## 193.1.0

### New features

* Added FrozenDictionary and FrozenHashSet to sandbox whitelist
* Added yaml type serializers for FrozenDictionary and FrozenHashSet
* Added `IPrototypeManager.GetInstances<T>()`
* `IPrototypeManager` now also raises `PrototypesReloadedEventArgs` as a system event.

### Bugfixes

* Might fix some PVS bugs added in the last version.

### Internal

* Various static dictionaries have been converted into FrozenDictionary.


## 193.0.0

### Breaking changes

* The `TransformChildrenEnumerator`'s out values are now non-nullable

### New features

* Added `IPrototypeManager.TryGetInstances()`, which returns a dictionary of prototype instances for a given prototype kind/type.

### Bugfixes

* Fixed `BaseAudioSource.SetAuxiliary()` throwing errors on non-EFX systems

### Internal


* The internals of PVS system have been reworked to reduce the number of dictionary lookups.
* `RobustMappedStringSerializer` now uses frozen dictionaries
* `IPrototypeManager` now uses frozen dictionaries


## 192.0.0

### Breaking changes

* `EntitySystem.TryGetEntity` is now `protected`.

### Internal

* PVS message ack processing now happens asynchronously
* Dependency collections now use a `FrozenDictionary`


## 191.0.1

### Bugfixes

.* Fix sandbox being broken thanks to .NET 8.


## 191.0.0

### Breaking changes

* Robust now uses **.NET 8**. Nyoom.

### Bugfixes

* `IResourceCache.TryGetResource<T>` won't silently eat all exceptions anymore.


## 190.1.1

### Bugfixes

* Revert broadphase job to prevent OOM from logs.


## 190.1.0

### New features

* Add OnGrabbed / OnReleased to slider controls.
* Add Rotation method for matrices and also make the precision slightly better when angles are passed in by taking double-precision not single-precision floats.

### Bugfixes

* Fix some grid setting asserts when adding gridcomponent to existing maps.


## 190.0.0

### New features

* Add color gradients to sliders.

### Bugfixes

* Fix HSV / HSL producing black colors on 360 hue.
* Stop terminating entities from prematurely detaching to nullspace.
* Ensure shader parameters update when swapping instances.

### Other

* Add more verbose logging to OpenAL errors.

### Internal

* Change NetSyncEnabled to an assert and fix instances where it slips through to PVS.


## 189.0.0

### Breaking changes

* Use the base AudioParams for networking not the z-offset adjusted ones.
* Modulate SpriteView sprites by the control's color modulation.

### New features

* Improve YAML linter error messages for parent nodes.
* ExpandPvsEvent will also be raised directed to the session's attached entity.

### Bugfixes

* Client clientside entity error spam.

### Internal

* Set priorGain to 0 where no EFX is supported for audio rather than 0.5.
* Try to hotfix MIDI lock contention more via a semaphore.


## 188.0.0

### Breaking changes

* Return null buffered audio if there's an exception and use the dummy instance internally.
* Use entity name then suffix for entity spawn window ordering.
* Change MidiManager volume to gain.
* Remove EntityQuery from the MapVelocity API.

### Bugfixes

* Potentially fix some audio issues by setting gain to half where EFX not found and the prior gain was 0.
* Log errors upon trying to spawn audio attached to deleted entities instead of trying to spawn them and erroring later.
* Fixed predicted audio spawns not applying the adjusted audio params.
* Fix GetDimensions for the screenhandle where the text is only a single line.


## 187.2.0

### New features

* Added a cancellable bool to physics sleeping events where we may wish to cancel it.

### Bugfixes

* Fix corrupted physics awake state leading to client mispredicts.


## 187.1.2

### Bugfixes

* Hotfix contact nullrefs if they're modified during manifold generation.


## 187.1.1

### Bugfixes

* Revert physics solver job to fix crashes until box2d v3 rolls around.
* Don't RegenerateContacts if the body isn't collidable to avoid putting non-collidable proxies on the movebuffer.


## 187.1.0

### Bugfixes

* Apply default audio params to all audio sources not just non-buffered ones.
* Avoid re-allocating broadphase job every tick and maybe fix a rare nullref for it.


## 187.0.0

### New features

* Improved error message for network failing to initialize.

### Bugfixes

* Fix not being able to add multiple PVS session overrides in a single tick without overwriting each one. This should fix issues with audio filters.

### Other

* Changed toolshed initialisation logs to verbose.


## 186.1.0

### New features

* Add public method to get PVS session overrides for a specific session.

### Internal

* Add temporary audio debugging.


## 186.0.0

### Breaking changes

* Global audio is now stored on its own map to avoid contamination issues with nullspace.

### Bugfixes

* Fix MIDIs playing cross-map
* Only dispose audio on game closure and don't stop playing if it's disposed elsewhere i.e. MIDIs.


## 185.2.0

### Bugfixes

* Bandaid deleted MIDI source entities spamming velocity error logs.

### Other

* Reverted MIDI audio not updating every frame due to lock contention with the MIDI renderer for now.


## 185.1.1

### Bugfixes

* Fix Z-Offset for audio not being applied on initialization.

### Internal

* Flag some internal queries as approximate to avoid unnecessary AABB checks. Some of these are already covered off with TestOverlap calls and the rest will need updating to do so in a future update.


## 185.1.0

### New features

* Audio listener's velocity is set using the attached entity's velocity rather than ignored.

### Bugfixes

* Fix imprecision on audio position


## 185.0.0

### Breaking changes

* Added a flag for grid-based audio rather than implicitly doing it.

### New features

* Added IRobustJob and IParallelRobustJob (which splits out into IRobustJob). These can be passed to ParallelManager for work to be run on the threadpool without relying upon Task.Run / Parallel.For which can allocate significantly more. It also has conveniences such as being able to specify batch sizing via the interface implementation.


## 184.1.0

### New features

* Add API to get gain / volume for a provided value on SharedAudioSystem.
* Make GetOcclusion public for AudioSystem.
* Add SharedAudioSystem.SetGain to complement SharedAudioSystem.SetVolume


## 184.0.1

### Bugfixes

* Update MIDI position and occlusion every frame instead of at set intervals.
* Fix global audio not being global.


## 184.0.0

### Internal

* Add RobustMemoryManager with RecyclableIOMemoryStream to significantly reduce MsgState allocations until better memory management is implemented.


## 183.0.0

### Breaking changes

* Audio rework has been re-merged now that the issues with packaging on server have been rectified (thanks PJB!)
* Reverted Arch pending further performance work on making TryGetComponent competitive with live.


## 182.1.1

### Internal

* Remove AggressiveInlining from Arch for debugging.


## 182.1.0

### New features

* Add IRobustRandom.SetSeed

### Other

* Add Arch.TrimExcess() back to remove excess archetypes on map load / EntityManager flush.


## 182.0.0

### Breaking changes

* Add EntityUid's generation / version to the hashcode.


## 181.0.2

### Bugfixes

* Fix exceptions from having too many lights on screen and causing the game to go black.
* Fix components having events raised in ClientGameStateManager before fully set and causing nullable reference exceptions.
* Replace tile intersection IEnumerables with TileEnumerator internally. Also made it public for external callers that wish to avoid IEnumerable.


## 181.0.1

### Bugfixes

* Fix the non-generic HasComp and add a test for good measure.


## 181.0.0

### Breaking changes

- Arch is merged refactoring how components are stored on engine. There's minimal changes on the API end to facilitate component nullability with much internal refactoring.


## 180.2.1


## 180.2.0

### New features

* Add EnsureEntity variants that take in collections.
* Add more MapSystem helper methods.

### Internal

* Cache some more PVS data to avoid re-allocating every tick.


## 180.1.0

### New features

* Add the map name to lsmap.
* Add net.pool_size to CVars to control the message data pool size in Lidgren and to also toggle pooling.

### Bugfixes

* Fix physics contraints causing enormous heap allocations.
* Fix potential error when writing a runtime log.
* Fix shape lookups for non-hard fixtures in EntityLookupSystem from 180.0.0


## 180.0.0

### Breaking changes

* Removed some obsolete methods from EntityLookupSystem.

### New features

* PhysicsSystem.TryGetNearest now supports chain shapes.
* Add IPhysShape methods to EntityLookupSystem rather than relying on AABB checks.
* Add some more helper methods to SharedTransformSystem.
* Add GetOrNew dictionary extension that also returns a bool on whether the key existed.
* Add a GetAnchoredEntities overload that takes in a list.

### Other

* Use NetEntities for the F3 debug panel to align with command usage.


## 179.0.0

### Breaking changes

* EyeComponent.Eye is no longer nullable

### New features

* Light rendering can now be enabled or disable per eye.

### Bugfixes

* Deserializing old maps with empty grid chunks should now just ignore those chunks.

### Other

* UnknownPrototypeException now also tells you the prototype kind instead of just the unkown ID.
* Adding or removing networked components while resetting predicted entities now results in a more informative exception.


## 178.0.0

### Breaking changes

* Most methods in ActorSystem have been moved to ISharedPlayerManager.
* Several actor/player related components and events have been moved to shared.

### New features

* Added `NetListAsArray<T>.Value` to the sandbox whitelist


## 177.0.0

### Breaking changes

* Removed toInsertXform and added containerXform in SharedContainerSystem.CanInsert.
* Removed EntityQuery parameters from SharedContainerSystem.IsEntityOrParentInContainer.
* Changed the signature of ContainsEntity in SharedTransformSystem to use Entity<T>.
* Removed one obsoleted SharedTransformSystem.AnchorEntity method.
* Changed signature of SharedTransformSystem.SetCoordinates to use Entity<T>.

### New features

* Added more Entity<T> query methods.
* Added BeforeApplyState event to replay playback.

### Bugfixes

* Fixed inverted GetAllMapGrids map id check.
* Fixed transform test warnings.
* Fixed PlacementManager warnings.
* Fixed reparenting bug for entities that are being deleted.

### Other

* Changed VerticalAlignment of RichTextLabel to Center to be consistent with Label.
* Changed PVS error log to be a warning instead.
* Marked insert and remove container methods as obsolete, added container system methods to replace them.
* Marked TransformComponent.MapPosition as obsolete, added GetMapCoordinates system method to replace it.

### Internal

* Moved TryGetUi/TryToggleUi/ToggleUi/TryOpen/OpenUi/TryClose/CloseUi methods from UserInterfaceSystem to SharedUserInterfaceSystem.


## 176.0.0

### Breaking changes

* Reverted audio rework temporarily until packaging is fixed.
* Changes to Robust.Packaging to facilitate Content.Packaging ports from the python packaging scripts.

### New features

* Add a cvar for max game state buffer size.
* Add an overload for GetEntitiesInRange that takes in a set.

### Bugfixes

* Fix PVS initial list capacity always being 0.
* Fix replay lerp error spam.


## 175.0.0

### Breaking changes

* Removed static SoundSystem.Play methods.
* Moved IPlayingAudioStream onto AudioComponent and entities instead of an abstract stream.
* IResourceCache is in shared and IClientResourceCache is the client version to use for textures.
* Default audio attenuation changed from InverseDistanceClamped to LinearDistanceClamped.
* Removed per-source audio attenuation.

### New features

* Add preliminary support for EFX Reverb presets + auxiliary slots; these are also entities.
* Audio on grid entities is now attached to the grid.

### Bugfixes

* If an audio entity comes into PVS range its track will start at the relevant offset and not the beginning.
* Z-Axis offset is considered for ReferenceDistance / MaxDistance for audio.
* Audio will now pause if the attached entity is paused.

### Other

* Changed audio Z-Axis offset from -5m to -1m.


## 174.0.0

### Breaking changes

* ActorComponent has been moved to `Robust.Shared.Player` (namespace changed).

### New features

* Added `SpriteSystem.GetFrame()` method, which takes in an animated RSI and a time and returns a frame/texture.
* Added `IRobustRandom.NextAngle()`


## 173.1.0

### New features

* Add physics chain shapes from Box2D.


## 173.0.0

### Breaking changes

* Remove GridModifiedEvent in favor of TileChangedEvent.

### Bugfixes

* Fix some grid rendering bugs where chunks don't get destroyed correctly.


## 172.0.0

### Breaking changes

* Remove TryLifestage helper methods.
* Refactor IPlayerManager to remove more IPlayerSession, changed PlayerAttachedEvent etc on client to have the Local prefix, and shuffled namespaces around.

### New features

* Add EnsureComponent(ref Entity<\T?>)

### Bugfixes

* Re-add force ask threshold and fix other PVS bugs.


## 171.0.0

### Breaking changes

* Change PlaceNextTo method names to be more descriptive.
* Rename RefreshRelay for joints to SetRelay to match its behaviour.

### Bugfixes

* Fix PVS error spam for joint relays not being cleaned up.

### Other

* Set EntityLastModifiedTick on entity spawn.


## 170.0.0

### Breaking changes

* Removed obsolete methods and properties in VisibilitySystem, SharedContainerSystem and MetaDataComponent.

### Bugfixes

* Fixed duplicate command error.
* Fixed not being able to delete individual entities with the delete command.

### Other

* FileLogHandler logs can now be deleted while the engine is running.


## 169.0.1

### Other

* The client now knows about registered server-side toolshed commands.

## 169.0.0

### Breaking changes

* Entity<T> has been introduced to hold a component and its owning entity. Some methods that returned and accepted components directly have been removed or obsoleted to reflect this.

### Other

* By-value events may now be subscribed to by-ref.
* The manifest's assemblyPrefix value is now respected on the server.


## 168.0.0

### Breaking changes

* The Component.OnRemove method has been removed. Use SubscribeLocalEvent<TComp, ComponentRemove>(OnRemove) from an EntitySystem instead.


## 167.0.0

### Breaking changes

* Remove ComponentExtensions.
* Remove ContainerHelpers.
* Change some TransformSystem methods to fix clientside lerping.

### Bugfixes

* Fixed PVS bugs from dropped entity states.

### Other

* Add more joint debug asserts.


## 166.0.0

### Breaking changes

* EntityUid-NetEntity conversion methods now return null when given a null value, rather than returning an invalid id.
* ExpandPvsEvent now defaults to using null lists to reduce allocations.
* Various component lifestage related methods have been moved from the `Component` class to `EntityManager`.
* Session/client specific PVS overrides are now always recursive, which means that all children of the overriden entity will also get sent.

### New features

* Added a SortedSet yaml serializer.

### Other

* AddComponentUninitialized is now marked as obsolete and will be removed in the future.
* DebugTools.AssertOwner() now accepts null components.


## 165.0.0

### Breaking changes

* The arguments of `SplitContainer`s resize-finished event have changed.

### New features

* The YAML validator now checks the default values of ProtoId<T> and EntProtoId data fields.

### Bugfixes

* The minimum draggable area of split containers now blocks mouse inputs.


## 164.0.0

### Breaking changes

* Make automatic component states infer cloneData.
* Removed cloneData from AutoNetworkedFieldAttribute. This is now automatically inferred.

### Internal

* Reduce Transform GetComponents in RecursiveDeleteEntity.


## 163.0.0

### Breaking changes

* Moved TimedDespawn to engine for a component that deletes the attached entity after a timer has elapsed.

### New features

* Add ExecuteCommand for integration tests.
* Allow adding / removing widgets of cub-controls.
* Give maps / grids a default name to help with debugging.
* Use ToPrettyString in component resolve errors to help with debugging.

### Bugfixes

* Fix console backspace exception.
* Fix rendering invalid maps spamming exceptions every frame.

### Internal

* Move ClientGameStatemanager local variables to fields to avoid re-allocating every tick.


## 162.2.1


## 162.2.0

### New features

* Add support for automatically networking entity lists and sets.
* Add nullable conversion operators for ProtoIds.
* Add LocId serializer for validation.

### Bugfixes

* Fix deleting a contact inside of collision events throwing.
* Localize VV.

### Internal

* Use CollectionsMarshal in GameStateManager.


## 162.1.1

### Bugfixes

* Fixes "NoSpawn" entities appearing in the spawn menu.


## 162.1.0

### New features

* Mark ProtoId as NetSerializable.

### Bugfixes

* Temporarily revert NetForceAckThreshold change as it can lead to client stalling.
* Fix eye visibility layers not updating on children when a parent changes.

### Internal

* Use CollectionsMarshal in RobustTree and AddComponentInternal.


## 162.0.0

### New features

* Add entity categories for prototypes and deprecate the `noSpawn` tag.
* Add missing proxy method for `TryGetEntityData`.
* Add NetForceAckThreshold cvar to forcibly update acks for late clients.

### Internal

* Use CollectionMarshals in PVS and DynamicTree.
* Make the proxy methods use MetaQuery / TransformQuery.


## 161.1.0

### New features

* Add more DebugTools assert variations.

### Bugfixes

* Don't attempt to insert entities into deleted containers.
* Try to fix oldestAck not being set correctly leading to deletion history getting bloated for pvs.


## 161.0.0

### Breaking changes

* Point light animations now need to use different component fields in order to animate the lights. `Enabled` should be replaced with `AnimatedEnable` and `Radius` should be replaced with `AnimatedRadius`

### New features

* EntProtoId is now net-serializable
* Added print_pvs_ack command to debug PVS issues.

### Bugfixes

* Fixes AngleTypeParser not using InvariantCulture
* Fixed a bug that was causing `MetaDataComponent.LastComponentRemoved` to be updated improperly.

### Other

* The string representation of client-side entities now looks nicer and simply uses a 'c' prefix.


## 160.1.0

### New features

* Add optional MetaDataComponent args to Entitymanager methods.

### Internal

* Move _netComponents onto MetaDataComponent.
* Remove some component resolves internally on adding / removing components.


## 160.0.2

### Other

* Transform component and containers have new convenience fields to make using VIewVariables easier.


## 160.0.0

### Breaking changes

* ComponentReference has now been entirely removed.
* Sensor / non-hard physics bodies are now included in EntityLookup by default.


## 159.1.0


## 159.0.3

### Bugfixes

* Fix potentially deleted entities having states re-applied when NetEntities come in.


## 159.0.2

### Bugfixes

* Fix PointLight state handling not queueing ComponentTree updates.


## 159.0.1

### Bugfixes

* Fix pending entity states not being removed when coming in (only on entity deletion).

### Internal

* Remove PhysicsComponent ref from Fixture.


## 159.0.0

### Breaking changes

* Remove ComponentReference from PointLights.
* Move more of UserInterfaceSystem to shared.
* Mark some EntitySystem proxy methods as protected instead of public.

### New features

* Make entity deletion take in a nullable EntityUid.
* Added a method to send predicted messages via BUIs.

### Other

* Add Obsoletions to more sourcegen serv4 methods.
* Remove inactive reviewers from CODEOWNERs.


## 158.0.0

### Breaking changes

* Remove SharedEyeComponent.
* Add Tile Overlay edge priority.


## 157.1.0

### New features

* UI tooltips now use rich text labels.


## 157.0.0

### Breaking changes

* Unrevert container changes from 155.0.0.
* Added server-client EntityUid separation. A given EntityUid will no longer refer to the same entity on the server & client.
* EntityUid is no longer net-serializable, use NetEntity instead, EntityManager & entity systems have helper methods for converting between the two,


## 156.0.0

### Breaking changes

* Revert container changes from 155.0.0.


## 155.0.0

### Breaking changes

* MapInitEvent now gets raised for components that get added to entities that have already been map-initialized.

### New features

* VirtualWritableDirProvider now supports file renaming/moving.
* Added a new command for toggling the replay UI (`replay_toggleui`).

### Bugfixes

* Fixed formatting of localization file errors.
* Directed event subscriptions will no longer error if the corresponding component is queued for deletion.


## 154.2.0



### New features

* Added support for advertising to multiple hubs simultaneously.
* Added new functions to ContainerSystem that recursively look for a component on a contained entity's parents.

### Bugfixes

* Fix Direction.TurnCw/TurnCcw to South returning Invalid.


## 154.1.0

### New features

* Add MathHelper.Max for TimeSpans.

### Bugfixes

* Make joint initialisation only log under IsFirstTimePredicted on client.

### Other

* Mark the proxy Dirty(component) as obsolete in line with EntityManager (Dirty(EntityUid, Component) should be used in its place).


## 154.0.0

### Breaking changes

* Change ignored prototypes to skip prototypes even if the prototype type is found.
* Moved IPlayerData interface to shared.

### New features

* Added a multiline text submit keybind function.

### Bugfixes

* Fixed multiline edits scrollbar margins.

### Internal

* Added more event sources.
* Made Toolshed types oneOff IoC injections.


## 153.0.0

### Breaking changes

* Removed SharedUserInterfaceComponent component references.
* Removed EntityDeletedMessage.

### Other

* Performance improvements for replay recording.
* Lidgren has been updated to [v0.2.6](https://github.com/space-wizards/SpaceWizards.Lidgren.Network/blob/v0.2.6/RELEASE-NOTES.md).
* Make EntityManager.AddComponent with a component instance set the owner if its default, add system proxy for it.

### Internal

* Added some `EventSource` providers for PVS and replay recording: `Robust.Pvs` and `Robust.ReplayRecording`.
* Added RecursiveMoveBenchmark.
* Removed redundant prototype resolving.
* Removed CollisionWake component removal subscription.
* Removed redundant DebugTools.AssertNotNull(netId) in ClientGameStateManager


## 152.0.0

### Breaking changes

* `Robust.Server.GameObjects.BoundUserInterface.InteractionRangeSqrd` is now a get-only property. Modify `InteractionRange` instead if you want to change it on active UIs.
* Remove IContainerManager.
* Remove and obsolete ComponentExt methods.
* Remove EntityStarted and ComponentDeleted C# events.
* Convert Tile.TypeId to an int. Old maps that were saved with TypeId being an ushort will still be properly deserialized.

### New features

* `BoundUserInterfaceCheckRangeEvent` can be used to implement custom logic for BUI range checks.
* Add support for long values in CVars.
* Allow user code to implement own logic for bound user interface range checks.

### Bugfixes

* Fix timers counting down slower than real time and drifting.
* Add missing System using statement to generated component states.
* Fix build with USE_SYSTEM_SQLITE.
* Fix prototype manager not being initialized in robust server simulation tests.
* Fix not running serialization hooks when copying non-byref data definition fields without a custom type serializer.

### Other

* Remove warning for glibc 2.37.
* Remove personally-identifiable file paths from client logs.

### Internal

* Disable obsoletion and inherited member hidden warnings in serialization source generated code.
* Update CI workflows to use setup-dotnet 3.2.0 and checkout 3.6.0.
* Fix entity spawn tests having instance per test lifecycle with a non static OneTimeTearDown method.
* Add new PVS test to check that there is no issue with entity states referencing other entities that the client is not yet aware of.


## 151.0.0


## 150.0.1

### Bugfixes

* Fix some partial datadefs.


## 150.0.0

### Breaking changes

* Remove the Id field from Fixtures as the Id is already stored on FixturesComponent.

### New features

* Add AbstractDictionarySerializer for abstract classes.
* Add many new spawn functions for entities for common operations.


## 149.0.1

### Bugfixes

* Fix serialization sharing instances when copying data definitions and not assigning null when the source is null.
* Fixed resizing a window to be bigger than its set maxsize crashing the client.


## 149.0.0

### Breaking changes

* Data definitions must now be partial, their data fields must not be readonly and their data field properties must have a setter.

### Internal

* Copying data definitions through the serialization manager is now faster and consumes less memory.


## 148.4.0

### New features

* Add recursive PVS overrides and remove IsOverride()


## 148.3.0

### New features

* Happy eyeballs delay can be configured.
* Added more colors.
* Allow pre-startup components to be shut down.
* Added tile texture reload command.
* Add implementation of Random.Pick(ValueList<T> ..).
* Add IntegrationInstance fields for common dependencies.

### Bugfixes

* Prevent invalid prototypes from being spawned.
* Change default value of EntityLastModifiedTick from zero to one.
* Make DiscordRichPresence icon CVars server-side with replication.


## 148.2.0

### New features

* `SpinBox.LineEditControl` exposes the underlying `LineEdit`.
* Add VV attributes to various fields across overlay and sessions.
* Add IsPaused to EntityManager to check if an entity is paused.

### Bugfixes

* Fix SetActiveTheme not updating the theme.


## 148.1.0

### New features

* Added IgnoreUIChecksComponent that lets entities ignore bound user interface range checks which would normally close the UI.
* Add support for F16-F24 keybinds.

### Bugfixes

* Fix gamestate bug where PVS is disabled.

### Other

* EntityQuery.HasComponent override for nullable entity uids.


## 148.0.0

### Breaking changes

* Several NuGet dependencies are now private assets.
* Added `IViewportControl.PixelToMap()` and `PixelToMapEvent`. These are variants of the existing screen-to-map functions that should account for distortion effects.

### New features

* Added several new rich-text tags, including italic and bold-italic.

### Bugfixes

* Fixed log messages for unknown components not working due to threaded IoC issues.
* Replay recordings no longer record invalid prototype uploads.


## 147.0.0

### Breaking changes

* Renamed one of the EntitySystem.Dirty() methods to `DirtyEntity()` to avoid confusion with the component-dirtying methods.

### New features

* Added debug commands that return the entity system update order.

### Bugfixes

* Fixed a bug in MetaDataSystem that was causing the metadata component to not be marked as dirty.


## 146.0.0

### Breaking changes

* Remove readOnly for DataFields and rename some ShaderPrototype C# fields internally to align with the normal schema.

### Bugfixes

* Add InvariantCulture to angle validation.

### Internal

* Add some additional EntityQuery<T> usages and remove a redundant CanCollide call on fixture shutdown.


## 145.0.0

### Breaking changes

* Removed some old SpriteComponent data-fields ("rsi", and "layerDatums").

### New features

* Added `ActorSystem.TryGetActorFromUserId()`.
* Added IPrototypeManager.EnumerateKinds().

### Bugfixes

* Fixed SpriteSpecifierSerializer yaml validation not working properly.
* Fixed IoC/Threading exceptions in `Resource.Load()`.
* Fixed `TransformSystem.SetCoordinates()` throwing uninformative client-side errors.
* Fixed `IResourceManager.ContentFileExists()` and `TryContentFileRead()` throwing exceptions on windows when trying to open a directory.


## 144.0.1

### Bugfixes

* Fix some EntityLookup queries incorrectly being double transformed internally.
* Shrink TileEnlargement even further for EntityLookup default queries.


## 144.0.0

### Breaking changes

* Add new args to entitylookup methods to allow for shrinkage of tile-bounds checks. Default changed to shrink the grid-local AABB by the polygon skin to avoid clipping neighboring tile entities.
* Non-hard fixtures will no longer count by default for EntityLookup.

### New features

* Added new EntityLookup flag to return non-hard fixtures or not.


## 143.3.0

### New features

* Entity placement and spawn commands now raise informative events that content can handle.
* Replay clients can now optionally ignore some errors instead of refusing to load the replay.

### Bugfixes

* `AudioParams.PlayOffsetSecond` will no longer apply an offset that is larger then the length of the audio stream.
* Fixed yaml serialization of arrays of virtual/abstract objects.


### Other

* Removed an incorrect gamestate debug assert.


## 143.2.0

### New features

* Add support for tests to load extra prototypes from multiple sources.

### Bugfixes

* Fix named toolshed command.
* Unsubscribe from grid rendering events on shutdown.

### Other

* Remove unnecessary test prototypes.


## 143.1.0

### New features

* Add locale support for grammatical measure words.

### Bugfixes

* Don't raise contact events for entities that were QueueDeleted during the tick.
* Exception on duplicate broadcast subscriptions as this was unsupported behaviour.

### Other

* Add VV ReadWrite to PhysicsComponent BodyStatus.


## 143.0.0

### New features


- Toolshed, a tacit shell language, has been introduced.
  - Use Robust.Shared.ToolshedManager to invoke commands, with optional input and output.
  - Implement IInvocationContext for custom invocation contexts i.e. scripting systems.


## 142.1.2

### Other

* Don't log an error on failing to resolve for joint relay refreshing.


## 142.1.1

### Bugfixes

* Fixed a bad debug assert in `DetachParentToNull()`


## 142.1.0

### New features

* `IHttpClientHolder` holds a shared `HttpClient` for use by content. It has Happy Eyeballs fixed and an appropriate `User-Agent`.
* Added `DataNode.ToString()`. Makes it easier to save yaml files and debug code.
* Added some cvars to modify discord rich presence icons.
* .ogg files now read the `Artist` and `Title` tags and make them available via new fields in `AudioStream`.
* The default fragment shaders now have access to the local light level (`lowp vec3 lightSample`).
* Added `IPrototypeManager.ValidateAllPrototypesSerializable()`, which can be used to check that all currently loaded prototypes can be serialised & deserialised.

### Bugfixes

* Fix certain debug commands and tools crashing on non-SS14 RobustToolbox games due to a missing font.
* Discord rich presence strings are now truncated if they are too long.
* Fixed a couple of broadphase/entity-lookup update bugs that were affecting containers and entities attached to other (non-grid/map) entities.
* Fixed `INetChannel.Disconnect()` not properly disconnecting clients in integration tests.

### Other

* Outgoing HTTP requests now all use Happy Eyeballs to try to prioritize IPv6. This is necessary because .NET still does not support this critical feature itself.
* Made various physics related component properties VV-editable.
* The default EntitySystem sawmill log level now defaults to `Info` instead of `Verbose`. The level remains verbose when in debug mode.

### Internal

* The debug asserts in `DetachParentToNull()` are now more informative.


## 142.0.1

### Bugfixes

* Fix Enum serialization.


## 142.0.0

### Breaking changes

* `EntityManager.GetAllComponents()` now returns a (EntityUid, Component) tuple

### New features

* Added `IPrototypeManager.ValidateFields()`, which uses reflection to validate that the default values of c# string fields correspond to valid entity prototypes. Validates any fields with a `ValidatePrototypeIdAttribute`  and any data-field that uses the PrototypeIdSerializer custom type serializer.

### Other

* Replay playback will now log errors when encountering unhandled messages.
* Made `GetAssemblyByName()` throw descriptive error messages.
* Improved performance of various EntityLookupSystem functions


## 141.2.1

### Bugfixes

* Fix component trait dictionaries not clearing on reconnect leading to bad GetComponent in areas (e.g. entire game looks black due to no entities).


## 141.2.0

### Other

* Fix bug in `NetManager` that allowed exception spam through protocol abuse.


## 141.1.0

### New features

* MapInitEvent is run clientside for placementmanager entities to predict entity appearances.
* Add CollisionLayerChangeEvent for physics fixtures.


## 141.0.0

### Breaking changes

* Component.Initialize has been fully replaced with the Eventbus.

### Bugfixes

* Fixed potential crashes if buffered audio sources (e.g. MIDI) fail to create due to running out of audio streams.

### Other

* Pressing `^C` twice on the server will now cause it to hard-exit immediately.
* `Tools` now has `EXCEPTION_TOLERANCE` enabled.


## 140.0.0

### Breaking changes

* `IReplayRecordingManager.RecordingFinished` now takes a `ReplayRecordingFinished` object as argument.
* `IReplayRecordingManager.GetReplayStats` now returns a `ReplayRecordingStats` struct instead of a tuple. The units have also been normalized

### New features

* `IReplayRecordingManager` can now track a "state" object for an active recording.
* If the path given to `IReplayRecordingManager.TryStartRecording` is rooted, the base replay directory is ignored.

### Other

* `IReplayRecordingManager` no longer considers itself recording inside `RecordingFinished`.
* `IReplayRecordingManager.Initialize()` was moved to an engine-internal interface.


## 139.0.0

### Breaking changes

* Remove Component.Startup(), fully replacing it with the Eventbus.


## 138.1.0

### New features

* Add rotation methods to TransformSystem for no lerp.

### Bugfixes

* Fix AnimationCompleted ordering.


## 138.0.0

### Breaking changes

* Obsoleted unused `IMidiRenderer.VolumeBoost` property. Use `IMidiRenderer.VelocityOverride` instead.
* `IMidiRenderer.TrackedCoordinates` is now a `MapCoordinates`.

### New features

* Added `Master` property to `IMidiRenderer`, which allows it to copy all MIDI events from another renderer.
* Added `FilteredChannels` property to `IMidiRenderer`, which allows it to filter out notes from certain channels.
* Added `SystemReset` helper property to `IMidiRenderer`, which allows you to easily send it a SystemReset MIDI message.

### Bugfixes

* Fixed some cases were `MidiRenderer` would not respect the `MidiBank` and `MidiProgram.
* Fixed user soundfonts not loading.
* Fixed `ItemList` item selection unselecting everything when in `Multiple` mode.


## 137.1.0

### New features

* Added BQL `paused` selector.
* `ModUpdateLevel.PostInput` allows running content code after network and async task processing.

### Other

* BQL `with` now includes paused entities.
* The game loop now times more accurately and avoids sleeping more than necessary.
* Sandboxing (and thus, client startup) should be much faster when ran from the launcher.


## 137.0.0

### Breaking changes

* Component network state handler methods have been fully deprecated and replaced with the eventbus event equivalents (ComponentGetState and ComponentHandleState).


## 136.0.1

### Bugfixes

* Fixed debugging on Linux when CEF is enabled.


## 136.0.0

### New features

* Several more style box properties now scale with UI scale. Signature of some stylebox methods have been changed.

### Bugfixes

* Fixed OutputPanel scroll-bar not functioning properly.


## 135.0.0

### Breaking changes

* Style boxes now scale with the current UI scale. This affects how the the margins, padding, and style box textures are drawn and how controls are arranged. Various style box methods now need to be provided with the current UI scale.


## 134.0.0

### Breaking changes

* Several methods were moved out of the `UserInterface` components and into the UI system.
* The BUI constructor arguments have changed and now require an EntityUid to be given instead of a component.


## 133.0.0

### Breaking changes

* Replace Robust's Vector2 with System.Numerics.Vector2.

### New features

* `AssetPassPipe` has a new `CheckDuplicates` property that makes it explicitly check for and drop duplicate asset files passed through.

### Bugfixes

* Static entities that are parented to other entities will no longer collide with their parent.
* Fix some miscellaneous doc comments and typos (e.g. PvsSystem and EntityManager).
* Fix ContentGetDirectoryEntries.


## 132.2.0

### New features

* Add method to clear all joints + relayed joints on an entity.

### Other

* Lower default MTU to `1000`.

### Internal

* Resolved some warnings and unnecessary component resolves.


## 132.1.0

### New features

* `Robust.Shared.Physics.Events.CollisionChangeEvent` now has the `EntityUid` of the physics body.

### Other

* Paused entities now pause their animations. There's no guarantee they'll resume at the same point (use SyncSprite instead).

### Internal

* Fix ComponentTreeSystem warnings.
* Fix some miscellaneous other warnings.


## 132.0.1

### Bugfixes

* Return maps first from FindGridsIntersecting which fixes rendering order issues for grids.


## 132.0.0

### Breaking changes

* TimeOffsetSerializer now always reads & writes zeros unless it is reading/writing an initialized map. EntityPrototypes with TimeOffsetSerializer data-fields need to default to zero.\
* TimeOffsetSerializer now only applies a time offset when reading from yaml, not when copying.

### New features

* Added a function to count the number of prototypes of a given kind. See `IPrototypeManager.Count<T>()`.

### Bugfixes

* Fixed a bug in `IPrototypeManager.EnumerateParents()` that was causing it to not actually return the parent prototypes.

### Other

* Map serialisation will now log errors when saving an uninitialized map that contains initialized entities.


## 131.1.0

### New features

* Add NextByte method to random.
* Add method to get a random tile variant.

### Bugfixes

* Fix replay component state bug.

### Internal

* Remove some AggressiveOptimization attributes.


## 131.0.0

### Breaking changes

* `IWritableDirProvider` async functions have been removed.
* Replay recording & load API has been reworked to operate on zip files instead.
* Constants on `IReplayRecordingManager` have been moved to a new `ReplayConstants` class, renamed and values changed.

### New features

* Added `ISawmill.Verbose()` log functions.
* Replays are now written as `.zip` files. These will be [content bundles](https://docs.spacestation14.io/en/launcher/content-bundles) directly executable by the launcher if the server has the necessary build information.
* Client replays now use local time rather than UTC as default file name.


## 130.0.0

### Breaking changes

* Engine versions will no longer start with a leading 0.


## 0.129.0.1


## 129.0.0

### Breaking changes

* `AnchorSystem.Attach()` now behaves more like the obsolete `AttachToEntity()` methods as it will automatically detach a player from their current entity first.
* A chunk of server- and client-side `PrototypeLoadManager` code has been moved to shared.
* Replay recording and playback now supports client-side replays. Many replay related functions, cvars, and commands have changed.

### New features

* Richtext tags can now be overridden by content
* The LineEdit control now has a field to override the StyleBox
* `IWritableDirProvider` has new methods for async file writing.

### Bugfixes

* Updated Lidgren, fixing a bug where socket errors were not reported properly on Linux.

### Other

* The `Dirty()` method for networked components now has an override that takes  in an EntityUid. The old IEntityManager method being obsoleted.



## 0.128.0.0

### Breaking changes

* Add ILocalizationManager as a dependency on systems as `Loc`.


## 0.127.1.0

### New features

* Add SpriteSystem.Frame0 method for entity prototypes.


## 0.127.0.0

### Breaking changes

* Rename PVSSystem to PvsSystem.

### New features

* Added `launch.launcher` and `launch.content_bundle` CVars. These are intended to eventually replace the `InitialLaunchState` values.
* Allow `System.Net.IPAdress` through sandbox _properly_, add `System.Net.Sockets.AddressFamily` too.
* Systems now have their own logger sawmills automatically and can be access via `Log`.

### Bugfixes

* Make BoxContainer's MeasureOverride account for stretching.
* Fix IPAddress sandboxing.
* Revert physics contact getcomponents and also fix ShouldCollide ordering for PreventCollideEvent.


## 0.126.0.0

### Breaking changes

* Several `MapManager` methods were moved to `MapSystem`.
* The signature of grid lookup queries has changed, with a new optional `includeMap` bool added in-between other optional bools.

### New features

* `System.Net.IPAddress` is now accessible from the sandbox.

### Bugfixes

* Fixed RichText not rendering some tags properly for some UI scales.
* Text inside of `OutputPanel` controls should no longer overlap with the scrollbar.

### Other

* Obsoleted the following methods from `IPlayerSession`: `AttachToEntity`, `DetachFromEntity`. Use the methods in `ActorSystem` instead.
* Static Loggers (e.g., `Logger.Log()` are now obsoleted. Get a sawmill from ILogManager instead.
* Several `MetadataComponent` setters have been marked as obsolete. Use `MetaDataSystem` methods instead.

### Internal

* Removed several static logging calls.


## 0.125.0.1

### Other

* Use a logger sawmill in MapManager rather than the static logger.


## 0.125.0.0

### Breaking changes

* Several replay related cvars and commands have been renamed.

### New features

* Added support for basic replay playback. The API is likely to change in the next version or two.


## 0.124.0.1

### New features

* Added `CompletionHelper.ContentDirPath()`.
* Added `vfs_ls` command to list VFS contents.
* The resource manifest (`manifest.yml`) now accepts a `clientAssemblies` key. When given, only the assembly names listed will be loaded from `/Assemblies/` rather than automatically loading all assemblies found.

### Bugfixes

* Fix exception if running the `>` command (remote execute) without even a space after it.
* `ResPath.RelativeTo()` now considers non-rooted paths relative to `.`.
  * This fixes some things like `MemoryContentRoot`'s `FindFiles()` implementation.
* Fix `IContentRoot.GetEntries()` default implementation (used by all content roots except `DirLoader`) not working at all.
* Made `ResourceManager.ContentGetDirectoryEntries()` report content root mount paths as directories.

### Internal

* Made `ConfigurationManager` not-abstract anymore so we can instantiate it from tests.
* Added new tests for `ResourceManager`.


## 0.124.0.0

### Breaking changes

* PreventCollideEvent changes to align it with the other physics events.


## 0.123.1.1

### Bugfixes

* Also clone warmstarting data for joints in the physics solver.


## 0.123.1.0

### New features

* Add Box2.Rounded(int digits) method.
* Add Pure attributes to Box2 methods.


## 0.123.0.0

### New features

* Added `ValueList.RemoveSwap()`
* The Centroid property on polygon shapes is now available to content.

### Bugfixes

* Fixed keyboard events always propagating to the default viewport if `devwindow` is open.
* Fixed some map-manager queries not properly using the `approx` argument.

### Other

* Several build/version cvars are now replicated to clients, instead of being server exclusive.


## 0.122.0.0

### Breaking changes

* Obsolete some MapManager queries.
* Add EntityUid to some MapManager queries.


## 0.121.0.0

### Breaking changes

* Add replaying loading / reading.

### New features

* Add setter for PlayingStream that also updates source.
* Add IWritableDirProvider.OpenOSWindow.

### Bugfixes

* Fix component lookups not considering whether an entity is in a container and the flag is set.


## 0.120.0.0

### Breaking changes

* Relay contained joints to parents and no longer implicitly break them upon container changes.

### Bugfixes

* Fix upload folder command.
* Fix SpriteView scaling for aspect ratios.

### Internal

* Cleanup MapManager slightly.


## 0.119.0.1

### Bugfixes

* Fix non-hard kinematiccontroller fixtures not colliding.


## 0.119.0.0

### Breaking changes

* Move prototype upload commands to the engine.

### New features

* Add IContentRoot.FileExists(ResPath).


## 0.118.0.0

### Breaking changes

* ComponentRegistry has been re-namespaced.

### New features

* You can now provide a ComponentRegistry to SpawnEntity to override some components from the prototype.


## 0.117.0.0

### Breaking changes

* Deprecate some sprite methods and cleanup IconComponent.
* YAML Linter supports inheritance.


## 0.116.0.0

### Breaking changes

* Removed AppearanceVisualizers.
* Modify replay record directory selection.


## 0.115.0.0

### Breaking changes

* The signature and behaviour of `IClientGameStateManager.PartialStateReset()` has changed. By default it will no longer delete client-side entities, unless they are parented to a networked entity that is being deleted during the reset.


## 0.114.1.0

### New features

* Add a new method for physics joint removal.

### Other

* Slightly speedup entity deletion.

### Internal

* Remove static logs from EntityManager.


## 0.114.0.0

### Breaking changes

* The way that UI themes resolve textures has changed. Absolute texture paths will simply be read directly, while relative paths will attempt to find a theme specific texture before falling back to simply trying to read the given file path.
* The signature of public UI theme methods have changed, and some new methods have been added.

### New features

* Added non-generic versions of various component/entity lookup queries.

### Bugfixes

* Fixed an erroneous error that would get logged when clients reconnect to a server.
* Fixed a UI bug that was preventing some controls from being disposed and was causing the UI to become laggy.


## 0.113.0.3

### Bugfixes

* Fix PVS error log threading issue.


## 0.113.0.2

### Bugfixes

* Removed or fixed some erroneous debug asserts
* Fixed entity-deletion not being properly sent to clients


## 0.113.0.1

### Bugfixes

* Use ThemeResolve for TextureButton texture normals.


## 0.113.0.0

### Breaking changes

* Move JobQueue<T> from content to engine.

### New features

* Make InitializeEntity and StartEntity public. InitializeAndStartEntity was already public.

### Bugfixes

* Add padding to font glyphs in the atlas.
* Fix log for duplicate component references.
* Make Map-Grids set GridUid earlier.
* Fix hidden action numbers when updating UI theme.
* Fix joint change events subscribing to predictedphysics instead of just physics.

### Other

* Remove joint log as it's never been read and caused threading issues.
* Decouple vvwrite / vvread / vvinvoke perms slightly from vv so vv no longer implicitly grants the others.
* Add start line to duplicate prototype yaml error.
* Fix debug sprite assert.
* Fix some joint bugs


## 0.112.0.1


## 0.112.0.0

### Breaking changes

* Move default theme directory to /Interface/ from /UserInterface/
* Try to fix contact mispredicts with PredictedPhysicsComponent.

### Bugfixes

* Fix JSON Serialization of ResPath.

### Other

* Change prof tree style & add basic stylesheet support.


## 0.111.0.0

### Breaking changes

* Add default stylesheet for engine + debug connect screen.


## 0.110.0.0

### Breaking changes

* Remove name + authors from map files as these were unused and overwritten on every mapfile write.

### Bugfixes

* Fix Omnisharp failing to analyze the client by default.
* Fix EntityLookup not properly adding nested container entities.

### Other

* Sort NetSerializable types.
* Remove obsolete Fixture.Body references.


## 0.109.1.0

### New features

* Add "IsDefault" to EntityManager for basic checks on whether an entity has default prototype data.


## 0.109.0.0

### Breaking changes

* `BeforeSaveEvent` has been moved from `Robust.Server.Maps` to `Robust.Shared.Map.Events`

### New features

* Added `IMidiRenderer.ClearAllEvents()`, a new method that clears all scheduled midi events.
* Added a new event (`BeforeSaveEvent`) which gets raised before a map/entity gets serialized to yaml.
* Added a new `ROBUST_SOUNDFONT_OVERRIDE` environmental variable that can be used to override system soundfonts.

### Bugfixes

* Fixed `EndCollideEvent` not setting the EntityUid fields.
* Fixed a bug that would cause screen-space overlays to sometimes not be drawn.


## 0.108.0.0

### Breaking changes

* Physics fixtures are now serialized by id, fixture rather than as a list with ids attached.


## 0.107.0.1

### Bugfixes

* Fix bad logs on maploader not listing out bad prototypes.


## 0.107.0.0

### Breaking changes

* Pass in dependencies to LocalPlayer AttachEntity (was anyone even using this method?)

### Internal

* Light query changes for some optimisation.
* Remove Texture.White IoC resolves in a lot of rendering areas.


## 0.106.1.0

### New features

* Screen-space overlays now use call `BeforeDraw()` and can use the `RequestScreenTexture` and `OverwriteTargetFrameBuffer` options.
* Added the `LoadedMapComponent`. It can be used to identify maps created by loading them from a yml file.


### Other

* `GameShared` no longer has a finalizer that triggers in some cases like tests.


## 0.106.0.0

### Breaking changes

* Update map file schema validator for new format.
* TimeOffsetSerializer fixes to use serv3 copying.

### Bugfixes

* Fix ResPath null errors.
* Fix queued deletion error log on entitymanager shutdown.

### Other

* Added transform recursion check in debug.


## 0.105.1.0

### New features

* Add CompOrNull to the EntityQuery struct.
* Add basic maploader support for entity renaming.


## 0.105.0.0

### Breaking changes

* Removed server and shared sprite components.

### New features

* Add LayerExists to sprites for object keys (previously it was only integer keys).

### Bugfixes

* Fix placement overlay error and add exception tolerance to it.


## 0.104.1.0

### New features

* VV now automatically dirties components.

### Bugfixes

* Fix CompletionHelper paths having double // on the end.


## 0.104.0.0

### Breaking changes

* API Changes to SpriteView control to generalize it.


## 0.103.0.0

### Breaking changes

* Maps are now saved by prototype -> entities rather than as just entities. Maps are currently backwards compatible but this is liable to change.

### New features

* RobustServerSimulation is public and usable by content for tests or benchmarking.
* Add sf3 extension support to midis.

### Bugfixes

* Fix random.Prob inequality.

### Other

* Adjust centerpoint for spriteview sprites.
* Mark ComponentReference as obsolete.


## 0.102.1.0

### New features

* `echo` console command to echo things.
* Add some public methods to physics system for applying force/torque.

### Bugfixes

* Fix a NRE when no window icon is specified.

### Other

* Set console code page to UTF-8 explicitly on Windows to fix output of non-ASCII characters.


## 0.102.0.0

### Breaking changes

* Loading  maps with invalid entity UIDs should now log errors.

### New features

* The yaml linter should now error on duplicate entity prototypes

### Bugfixes

* Fix a PVS bug that could put one entity into two different PVS chunks.

### Other

* EntityUid indexing should now start at 1 when saving maps.


## 0.101.1.1

### Bugfixes

* Fix polygon deserialization leading to the last vert being 0,0.


## 0.101.1.0

### New features

* Added a mode to entity placement to allow replacing any existing entities on a tile.

### Other

* Re-order initialization so BroadcastRunLevel is run after userinterfacemanager PostInitialize.


## 0.101.0.0

### Breaking changes

* Port Quickhull from Box2D and replace GiftWrapping.
* Removed a lot of unused physics code.

### Bugfixes

* Fix damping for mouse joint.
* Fix Distance outputs for overlapping circles.


## 0.100.0.0

### Breaking changes

* `ILookupWorldBox2Component` has been removed. If an entity does not have fixtures/physics a `WorldAABBEvent` will now be raised.

### Bugfixes

* Fixes a concurrent hashset modification exception in PVS


## 0.99.0.0

### Breaking changes

* Revert the reversion of the ResPath removal from 0.98.0.0

### New features

* StartCollideEvent, EndCollideEvent, and physics contacts now have the relevant EntityUids.

### Bugfixes

* Remove initialization code that forced transform and physics components first.


## 0.98.0.0

### Breaking changes

* Revert bulk ResPath refactor due to instability.


## 0.97.1.1

### Bugfixes

* Fixed assembly paths being used having double //


## 0.97.1.0

### New features

* FastNoiseLite is now netserializable.
* PVS ack processing is now parallel and also improved grafana metrics for PVS.

### Other

* Add invalid broadphase check to EntityLookupSystem.
* Made NetGraph logarithmic.


## 0.97.0.0

### Breaking changes

* Fully replace ResourcePath (class) with ResPath (struct).

### Other

* Add stacktrace to transform logs.


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
