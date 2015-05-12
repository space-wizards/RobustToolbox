#define RADIUS 7
#define KERNEL_SIZE (RADIUS * 2 + 1)

uniform float weights[KERNEL_SIZE];
uniform vec2 offsets[KERNEL_SIZE];

uniform sampler2D colorMap;

varying vec2 TexCoord;

vec4 GaussianBlur()
{
    vec4 color = vec4(0,0,0,0);
    
    for (int i = 0; i < KERNEL_SIZE; ++i)
        color += texture2D(colorMap, TexCoord + offsets[i]) * weights[i];
        
    return color;
}

void main()
{
	gl_FragColor = GaussianBlur();
} 
