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
