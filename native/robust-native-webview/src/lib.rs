#[cfg(target_os = "macos")]
unsafe extern "C" {
    fn get_swizzled_idiot();
}

// Not actually called but I need a chain of reference so that the objc code is compiled in.
#[unsafe(no_mangle)]
pub unsafe extern "C" fn rt_native_webview_init() {
    unsafe { get_swizzled_idiot() };
}
