// Reference so that wgpu-native has its exports exposed.
pub use wgpu_native::wgpuCreateInstance;

pub use wesl_c::wesl_compile;

use mimalloc::MiMalloc;

#[global_allocator]
static GLOBAL: MiMalloc = MiMalloc;
