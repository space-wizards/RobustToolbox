#version 120
#define RADIUS 11
#define KERNEL_SIZE (RADIUS * 2 + 1)
 
uniform vec2 weights_offsets[KERNEL_SIZE];
uniform vec2 weights_offsets0;
uniform vec2 weights_offsets1;
uniform vec2 weights_offsets2;
uniform vec2 weights_offsets3;
uniform vec2 weights_offsets4;
uniform vec2 weights_offsets5;
uniform vec2 weights_offsets6;
uniform vec2 weights_offsets7;
uniform vec2 weights_offsets8;
uniform vec2 weights_offsets9;
uniform vec2 weights_offsets10;
uniform vec2 weights_offsets11;
uniform vec2 weights_offsets12;
uniform vec2 weights_offsets13;
uniform vec2 weights_offsets14;
uniform vec2 weights_offsets15;
uniform vec2 weights_offsets16;
uniform vec2 weights_offsets17;
uniform vec2 weights_offsets18;
uniform vec2 weights_offsets19;
uniform vec2 weights_offsets20;
uniform vec2 weights_offsets21;
uniform vec2 weights_offsets22;

uniform sampler2D colorMapTexture;

vec4 GaussianBlurVertical()
{
  vec2 weights_offsets[KERNEL_SIZE] = vec2[KERNEL_SIZE]
    (
		weights_offsets0,
		weights_offsets1,
		weights_offsets2,
		weights_offsets3,
		weights_offsets4,
		weights_offsets5,
		weights_offsets6,
		weights_offsets7,
		weights_offsets8,
		weights_offsets9,
		weights_offsets10,
		weights_offsets11,
		weights_offsets12,
		weights_offsets13,
		weights_offsets14,
		weights_offsets15,	
		weights_offsets16,
		weights_offsets17,
		weights_offsets18,
		weights_offsets19,
		weights_offsets20,
		weights_offsets21,
		weights_offsets22
 	);

    vec4 color = vec4(0,0,0,0);
    
    for (int i = 0; i < KERNEL_SIZE; ++i)
    {
        color += texture2D(colorMapTexture, vec2(gl_TexCoord[0].x, gl_TexCoord[0].y + weights_offsets[i].y)) *  weights_offsets[i].x; // M A G I K
    }   
    return color;
}

void main()
{
 	gl_FragColor = GaussianBlurVertical();
}
