/*layout (location = 0)*/ attribute vec3 aPos;

varying vec2 pos;

uniform mat4 projectionMatrix;
uniform mat4 lightMatrix;

void main()
{
    vec4 rel = lightMatrix * vec4(aPos, 1.0);
    gl_Position = projectionMatrix * rel;
    pos = rel.xy;
}
