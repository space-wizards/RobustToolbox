// This is the main vertex shader used for rendering of 2D sprites.
#version 410 core

// Vertex position.
layout (location = 0) in vec2 aPos;
// Texture coordinates.
layout (location = 1) in vec2 tCoord;
layout (location = 2) in float tArrayIndex;

out vec2 TexCoord;
out float ArrayIndex;

layout (std140) uniform projectionViewMatrices
{
    mat3 projectionMatrix;
    mat3 viewMatrix;
};

uniform mat3 modelMatrix;
uniform float modifyArrayIndex;
// Allows us to do texture atlassing with texture coordinates 0->1
// Input texture coordinates get mapped to this range.
uniform vec4 modifyUV;

void main()
{
    vec3 transformed = projectionMatrix * viewMatrix * modelMatrix * vec3(aPos, 1.0);
    gl_Position = vec4(transformed, 1.0);
    TexCoord = mix(modifyUV.xy, modifyUV.zw, tCoord);
    ArrayIndex = tArrayIndex * modifyArrayIndex;
}
