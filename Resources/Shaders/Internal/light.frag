#version 330 core

out vec4 FragColor;

const float LIGHTING_HEIGHT = 1;

// Position of the fragment, in world coordinates.
in vec2 worldPosition;

uniform vec4 lightColor;
// Position of the light, in world coordinates.
uniform vec2 lightCenter;
uniform float lightRange;
uniform float lightPower;

void main()
{
    vec2 diff = worldPosition - lightCenter;
    float dist = dot(diff, diff) + 1;

    float val = clamp((1 - clamp(sqrt(dist) / lightRange, 0, 1)) * (1 / (sqrt(dist + 1))), 0, 1);

    val *= lightPower;

    FragColor = vec4(lightColor.rgb, val);
}
