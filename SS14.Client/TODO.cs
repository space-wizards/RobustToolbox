/*
 * Ok this file's just a giant TODO list for the Godot port now.
 * SO HERE'S THE LIST:
 * Re-add cool splash screen. - Seems pretty hard, probably not happening.
 * Fix that HORRIBLE hack inside PathHelpers.ExecutableRelativeFile()
 * Resource Cache doesn't use VFS, so no ZIP support.
    Godot has a VFS internally (See FileAccess and PackedData), but it's not exposed to user code afaict.
 * Client Eyes/Cameras
 * Export of client AND automatic fetching of SS14.Shared.Bsdiff for development.
 *  Really just needs a Windows build of the bsdiffwrap.dll for the second point.
 * Spawn Entities and Spawn Tiles.
 * Tilemaps don't seem to be clearing correctly on disconnect.
 */
