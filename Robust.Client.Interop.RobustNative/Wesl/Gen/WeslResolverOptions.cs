namespace Robust.Client.Interop.RobustNative.Wesl.Gen;

internal unsafe partial struct WeslResolverOptions
{
    public void* userdata;

    [NativeTypeName("WeslResolveSourceFunction")]
    public delegate* unmanaged[Cdecl]<sbyte*, void*, WeslResolveSourceResult*> resolve_source;

    [NativeTypeName("WeslResolveSourceFreeFunction")]
    public delegate* unmanaged[Cdecl]<WeslResolveSourceResult*, void*, void> resolve_source_free;

    [NativeTypeName("WeslResolveModuleFunction")]
    public delegate* unmanaged[Cdecl]<sbyte*, void*, WeslResolveModuleResult*> resolve_module;

    [NativeTypeName("WeslResolveModuleFreeFunction")]
    public delegate* unmanaged[Cdecl]<WeslResolveModuleResult*, void*, void> resolve_module_free;

    [NativeTypeName("WeslResolveStringFunction")]
    public delegate* unmanaged[Cdecl]<sbyte*, void*, sbyte*> display_name;

    [NativeTypeName("WeslResolveFreeStringFunction")]
    public delegate* unmanaged[Cdecl]<sbyte*, void*, void> free_display_name;

    [NativeTypeName("WeslResolveStringFunction")]
    public delegate* unmanaged[Cdecl]<sbyte*, void*, sbyte*> fs_path;

    [NativeTypeName("WeslResolveFreeStringFunction")]
    public delegate* unmanaged[Cdecl]<sbyte*, void*, void> free_fs_path;
}
