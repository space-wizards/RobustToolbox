// Group 0: global constants.
struct UniformConstants {
    time: f32
}

@group(0) @binding(0) var<uniform> Constants: UniformConstants;

// Group 1: parameters that change infrequently in a draw pass.
struct UniformView {
    projViewMatrix: mat3x2f
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
    // TODO: Pixel snapping?

    var out: VertexOutput;
    out.position = vec4(View.projViewMatrix * vec3(input.position, 1.0), 0.0, 1.0);
    out.texCoord = input.texCoord;
    out.color    = input.color;
    return out;
}

@fragment
fn fs_main(input: VertexOutput) -> @location(0) vec4f {
    var color = textureSample(mainTexture, mainSampler, input.texCoord);
    color = color * input.color;

    return color;
}
