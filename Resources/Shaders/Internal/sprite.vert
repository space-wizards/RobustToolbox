// This is the main vertex shader used for rendering of 2D sprites.
#version 330 core

// Vertex position.
layout (location = 0) in vec2 aPos;
// Texture coordinates.
layout (location = 1) in vec2 tCoord;

out vec2 UV;

// Maybe we should merge these CPU side.
// idk yet.
uniform mat3 modelMatrix;
layout (std140) uniform projectionViewMatrices
{
    mat3 projectionMatrix;
    mat3 viewMatrix;
};
// Allows us to do texture atlassing with texture coordinates 0->1
// Input texture coordinates get mapped to this range.
uniform vec4 modifyUV;

void main()
{
    vec3 transformed = projectionMatrix * viewMatrix * modelMatrix * vec3(aPos, 1.0);
    gl_Position = vec4(transformed.xy, 0.0, 1.0);
    UV = mix(modifyUV.xy, modifyUV.zw, tCoord);
}
