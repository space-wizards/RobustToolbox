// Group 0: global constants.
struct UniformConstants {
    time: f32
}

@group(0) @binding(0) var<uniform> Constants: UniformConstants;

// Group 1: parameters that change infrequently in a draw pass.
struct UniformView {
    projViewMatrix: mat2x3f,
    screenPixelSize: vec2f
}

@group(1) @binding(0) var<uniform> View: UniformView;


// Group 2: per-draw parameters.
@group(2) @binding(0)
var mainTexture: texture_2d<f32>;
@group(2) @binding(1)
var mainSampler: sampler;


struct VertexInput {
    @location(0) position: vec2f,
    @location(1) texCoord: vec2f,
    @location(2) color: vec4f
}

struct VertexOutput {
    @builtin(position) position: vec4f,
    @location(0) texCoord: vec2f,
    @location(1) color: vec4f,
}

@vertex
fn vs_main(input: VertexInput) -> VertexOutput {
    var transformed = vec3(input.position, 1.0) * View.projViewMatrix;

    transformed += 1.0;
    transformed /= View.screenPixelSize * 2.0;
    transformed = floor(transformed + 0.5);
    transformed *= View.screenPixelSize * 2.0;
    transformed -= 1.0;

    var out: VertexOutput;
    out.position = vec4(transformed, 0.0, 1.0);
    out.texCoord = input.texCoord;
    out.color    = srgb_to_linear(input.color);
    return out;
}

@fragment
fn fs_main(input: VertexOutput) -> @location(0) vec4f {
    var color = textureSample(mainTexture, mainSampler, input.texCoord);
    color = color * input.color;

    return color;
}

fn srgb_to_linear(srgb: vec4f) -> vec4f {
    let higher = pow((srgb.rgb + 0.055) / 1.055, vec3(2.4));
    let lower = srgb.rgb / 12.92;
    let s = max(vec3(0.0), sign(srgb.rgb - 0.04045));
    return vec4(mix(lower, higher, s), srgb.a);
}
