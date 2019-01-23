// This is the main fragment shader used for rendering of 2D sprites.
#version 450 core

out vec4 FragColor;

in vec2 TexCoord;

uniform sampler2D ourTexture;

void main()
{
    FragColor = texture(ourTexture, TexCoord);
}
