import Robust::SpriteBatch::{VertexInput, VertexOutput, mainTexture, mainSampler, View};
import Robust::Math::srgb_to_linear;

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
