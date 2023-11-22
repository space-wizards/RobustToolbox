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
