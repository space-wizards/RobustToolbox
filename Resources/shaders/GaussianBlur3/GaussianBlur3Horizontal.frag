#version 120
#define RADIUS 3
#define KERNEL_SIZE (RADIUS * 2 + 1)

uniform vec2 weights_offsets[KERNEL_SIZE];
uniform vec2 weights_offsets0;
uniform vec2 weights_offsets1;
uniform vec2 weights_offsets2;
uniform vec2 weights_offsets3;
uniform vec2 weights_offsets4;
uniform vec2 weights_offsets5;
uniform vec2 weights_offsets6;

uniform sampler2D colorMapTexture;

vec4 GaussianBlurHorizontal()
{
   vec4 color = vec4(0,0,0,0);
   
   vec2 weights_offsets[KERNEL_SIZE] = vec2[KERNEL_SIZE]
    (
		weights_offsets0,
		weights_offsets1,
		weights_offsets2,
		weights_offsets3,
		weights_offsets4,
		weights_offsets5,
		weights_offsets6
	); 
	
    for (int i = 0; i < KERNEL_SIZE; ++i)
        color += texture2D(colorMapTexture, vec2(gl_TexCoord[0].x + weights_offsets[i].y, gl_TexCoord[0].y)) * weights_offsets[i].x;
        
    return color;
}
void main()
{	
   gl_FragColor = GaussianBlurHorizontal();
}

