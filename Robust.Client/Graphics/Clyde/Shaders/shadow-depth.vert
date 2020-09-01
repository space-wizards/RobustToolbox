/*layout (location = 0)*/ attribute vec3 aPos;

varying vec2 pos;

// Note: This is *not* the standard projectionMatrix!
uniform mat4 shadowProjectionMatrix;
uniform mat4 shadowLightMatrix;

void main()
{
    vec4 rel = shadowLightMatrix * vec4(aPos, 1.0);
    gl_Position = shadowProjectionMatrix * rel;
    pos = rel.xy;
}
