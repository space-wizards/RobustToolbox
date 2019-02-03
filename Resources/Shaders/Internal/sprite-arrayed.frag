// This is the main fragment shader used for rendering of 2D sprites.
#version 410 core

out vec4 FragColor;

in vec2 TexCoord;
in float ArrayIndex;

uniform sampler2DArray ourTexture;
uniform vec4 modulate;

void main()
{
    FragColor = texture(ourTexture, vec3(TexCoord, ArrayIndex)) * modulate;
}
