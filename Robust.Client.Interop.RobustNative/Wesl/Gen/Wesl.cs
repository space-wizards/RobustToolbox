using System.Runtime.InteropServices;

namespace Robust.Client.Interop.RobustNative.Wesl.Gen;

internal static unsafe partial class Wesl
{
    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WeslCompiler* wesl_create_compiler();

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wesl_destroy_compiler(WeslCompiler* compiler);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WeslResult wesl_compile([NativeTypeName("const WeslStringMap *")] WeslStringMap* files, [NativeTypeName("const char *")] sbyte* root, [NativeTypeName("const WeslCompileOptions *")] WeslCompileOptions* options, [NativeTypeName("const WeslStringArray *")] WeslStringArray* keep, [NativeTypeName("const WeslBoolMap *")] WeslBoolMap* features);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WeslParseResult wesl_parse([NativeTypeName("const char *")] sbyte* source);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WeslResult wesl_eval([NativeTypeName("const WeslStringMap *")] WeslStringMap* files, [NativeTypeName("const char *")] sbyte* root, [NativeTypeName("const char *")] sbyte* expression, [NativeTypeName("const WeslCompileOptions *")] WeslCompileOptions* options, [NativeTypeName("const WeslBoolMap *")] WeslBoolMap* features);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern WeslExecResult wesl_exec([NativeTypeName("const WeslStringMap *")] WeslStringMap* files, [NativeTypeName("const char *")] sbyte* root, [NativeTypeName("const char *")] sbyte* entrypoint, [NativeTypeName("const WeslCompileOptions *")] WeslCompileOptions* options, [NativeTypeName("const WeslBindingArray *")] WeslBindingArray* resources, [NativeTypeName("const WeslStringMap *")] WeslStringMap* overrides, [NativeTypeName("const WeslBoolMap *")] WeslBoolMap* features);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wesl_free_string([NativeTypeName("const char *")] sbyte* ptr);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wesl_free_result(WeslResult* result);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wesl_free_exec_result(WeslExecResult* result);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    public static extern void wesl_free_translation_unit(WeslTranslationUnit* unit);

    [DllImport("robust-native", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: NativeTypeName("const char *")]
    public static extern sbyte* wesl_version();
}
