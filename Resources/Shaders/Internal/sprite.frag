#version 330 core

out vec4 FragColor;

in vec2 UV;

uniform sampler2D TEXTURE;
uniform sampler2D lightMap;
uniform vec4 modulate;

layout (std140) uniform uniformConstants
{
    vec2 SCREEN_PIXEL_SIZE;
    float TIME;
};

uniform vec2 TEXTURE_PIXEL_SIZE;

[SHADER_HEADER_CODE]

void main()
{
    vec4 FRAGCOORD = gl_FragCoord;

    vec4 COLOR;

    [SHADER_CODE]

    vec2 lightSampleUv = (FRAGCOORD.xy * SCREEN_PIXEL_SIZE);
    vec3 lightSample = texture(lightMap, lightSampleUv).rgb;

    FragColor = COLOR * modulate * vec4(lightSample, 1);
}
