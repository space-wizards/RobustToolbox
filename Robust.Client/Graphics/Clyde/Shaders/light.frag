#version 330 core

out vec4 FragColor;

const float LIGHTING_HEIGHT = 1;
const float PI = 3.1415926535897932384626433; // That enough digits?

// Position of the fragment, in world coordinates.
in vec2 worldPosition;
in vec2 UV;

uniform vec4 lightColor;
// Position of the light, in world coordinates.
uniform vec2 lightCenter;
uniform float lightRange;
uniform float lightPower;
uniform sampler2D lightMask;
uniform sampler2D shadowMap;
uniform int lightIndex;
uniform mat4 shadowMatrix;

void main()
{
    float mask = texture(lightMask, UV).r;

    vec2 diff = worldPosition - lightCenter;
    float realDist = dot(diff, diff);
    float dist = realDist + LIGHTING_HEIGHT;

    // SHADOWS
    float angle = atan(diff.y, -diff.x) + PI + radians(135.0);

    angle = mod(angle, 2 * PI);

    vec2 shadowPoint;
    float shadowMapOffset;

    if (angle < radians(90.0)) // Top
    {
        shadowPoint = diff;
        shadowMapOffset = 0;
    }
    else if (angle < radians(180.0)) // Right
    {
        shadowPoint = vec2(-diff.y, diff.x);
        shadowMapOffset = 0.25;
    }
    else if (angle < radians(270.0)) // Bottom
    {
        shadowPoint = -diff;
        shadowMapOffset = 0.50;
    }
    else // Left
    {
        shadowPoint = vec2(diff.y, -diff.x);
        shadowMapOffset = 0.75;
    }

    vec4 s = shadowMatrix * vec4(shadowPoint, 0.0, 1.0);
    s.xyz /= s.w;
    float su = s.x * 0.5 + 0.5;
    float sz = s.z * 0.5 + 0.5;

    float y = (lightIndex / 64.0) + (0.5 / 64.0);
    float shadowDepth = texture(shadowMap, vec2(su / 4 + shadowMapOffset, y)).r;


    if (shadowDepth < sz)
    {
        discard;
    }

    float val = clamp((1 - clamp(sqrt(dist) / lightRange, 0, 1)) * (1 / (sqrt(dist + 1))), 0, 1);

    val *= lightPower;
    val *= mask;

    FragColor = vec4(lightColor.rgb, val);
}
