fn srgb_to_linear(srgb: vec4f) -> vec4f {
    let higher = pow((srgb.rgb + 0.055) / 1.055, vec3(2.4));
    let lower = srgb.rgb / 12.92;
    let s = max(vec3(0.0), sign(srgb.rgb - 0.04045));
    return vec4(mix(lower, higher, s), srgb.a);
}
