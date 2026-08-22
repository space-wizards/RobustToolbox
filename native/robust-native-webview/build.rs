use std::{env, path::PathBuf};

fn main() {
    let target_os = env::var("CARGO_CFG_TARGET_OS").unwrap_or("".into());
    let cef_path = PathBuf::from(env::var_os("CEF_PATH").expect("CEF_PATH is not set"));

    if target_os == "macos" {
        cc::Build::new()
            .include(cef_path)
            .cpp(true)
            .flag("--std=c++17")
            .file("src/mac_application.mm")
            .link_lib_modifier("+whole-archive")
            .warnings(false)
            .compile("mac_application");

        println!("cargo::rerun-if-changed=src/mac_application.mm");
        println!("cargo::rustc-link-lib=framework=Cocoa");
    }
}
