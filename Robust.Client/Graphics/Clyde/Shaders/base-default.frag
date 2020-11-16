varying highp vec2 UV;
varying highp vec2 Pos;

uniform sampler2D lightMap;
uniform highp vec4 modulate;

#line 1000
// [SHADER_HEADER_CODE]

void main()
{
    highp vec4 FRAGCOORD = gl_FragCoord;

    lowp vec4 COLOR;

    #line 10000
    // [SHADER_CODE]

    lowp vec3 lightSample = texture2D(lightMap, Pos).rgb;

    gl_FragColor = zAdjustResult(COLOR * modulate * vec4(lightSample, 1.0));
}
