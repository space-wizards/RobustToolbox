varying highp vec2 UV;
varying highp vec2 Pos;
varying highp vec4 VtxModulate;

uniform sampler2D lightMap;

// [SHADER_HEADER_CODE]

void main()
{
    highp vec4 FRAGCOORD = gl_FragCoord;

    lowp vec4 COLOR;

    lowp vec3 lightSample = texture2D(lightMap, Pos).rgb;

    // [SHADER_CODE]

    // Whiten everything (even black source pixels) as lighting becomes too intense.
    vec3 overBrightC = max(vec3(0.0), lightSample - vec3(1.3));
    // Calculate a bright to white effect for strong lights
    float overBright = sqrt((overBrightC.r + overBrightC.g + overBrightC.b) * 0.33);
    vec3 bloomColor = min(vec3(1.0), vec3(0.002) * overBright + lightSample * overBright * 0.01);
    // Add the white component to a slight colored component.
    vec4 bloom = vec4(vec3(bloomColor), 0.0);
    // bloom = vec4(1.0);

    gl_FragColor = zAdjustResult(COLOR * VtxModulate * vec4(lightSample, 1.0) + bloom);
}
