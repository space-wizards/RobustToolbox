#define RADIUS 3
#define KERNEL_SIZE (RADIUS * 2 + 1)
 
uniform vec2 weights_offsets[KERNEL_SIZE];
uniform sampler2D colorMap;

varying vec2 TexCoord;

vec4 GaussianBlurVertical()
{
    vec4 color = vec4(0,0,0,0);
    
    for (int i = 0; i < KERNEL_SIZE; ++i)
        color += texture2D(colorMap, vec2(TexCoord.x, TexCoord.y + weights_offsets[i].y)) * weights_offsets[i].x;
        
    return color;
}



void main()
{
 	gl_FragColor = GaussianBlurVertical();
}