#ifndef HAS_VARYING_ATTRIBUTE
#define texture2D texture
#define varying in
#define attribute in
#define gl_FragColor colourOutput
out highp vec4 colourOutput;
#endif

varying highp vec2 UV;

uniform sampler2D tex;

void main()
{
    gl_FragColor = texture2D(tex, UV);
}
