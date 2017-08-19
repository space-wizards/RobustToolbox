#version 120
#define RADIUS 9
#define KERNEL_SIZE (RADIUS * 2 + 1)

uniform vec2 weights_offsets[KERNEL_SIZE];

uniform sampler2D colorMapTexture;

vec4 GaussianBlurVertical()
{
    vec4 color = vec4(0,0,0,0);

    for (int i = 0; i < KERNEL_SIZE; ++i)
        color += texture2D(colorMapTexture, vec2(gl_TexCoord[0].x, gl_TexCoord[0].y + weights_offsets[i].y)) * weights_offsets[i].x;

    return color;
}

void main()
{
    gl_FragColor = GaussianBlurVertical();
}
