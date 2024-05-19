varying highp vec2 UV;
varying highp vec2 UV2;
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

    gl_FragColor = zAdjustResult(COLOR * VtxModulate * vec4(lightSample, 1.0));
}
