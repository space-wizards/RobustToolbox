use std::ptr;

use cef::{App, ImplApp, ImplSchemeRegistrar, SchemeRegistrar, WrapApp, rc::Rc, wrap_app};

fn main() {
    #[cfg(target_os = "macos")]
    let _loader = {
        let loader =
            cef::library_loader::LibraryLoader::new(&std::env::current_exe().unwrap(), true);
        assert!(loader.load());
        loader
    };

    cef::api_hash(cef::sys::CEF_API_VERSION_14100, 0);

    let args = cef::args::Args::new();
    let main_args = args.as_main_args();

    let mut app = DemoApp::new();

    let ret = cef::execute_process(Some(main_args), Some(&mut app), ptr::null_mut());
    std::process::exit(ret)
}

// CefSchemeOptions
const SCHEME_STANDARD: i32 = 1 << 0;
const SCHEME_SECURE: i32 = 1 << 3;

wrap_app! {
    struct DemoApp {
    }

    impl App {
        fn on_register_custom_schemes(&self, registrar: Option<&mut SchemeRegistrar>) {
            let registrar = registrar.unwrap();
            // NOTE: KEEP IN SYNC WITH C# CODE!
            registrar.add_custom_scheme(Some(&"usr".into()), SCHEME_SECURE | SCHEME_STANDARD);
            registrar.add_custom_scheme(Some(&"res".into()), SCHEME_SECURE | SCHEME_STANDARD);
        }
    }
}
