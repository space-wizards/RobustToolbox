varying vec2 TexCoord;

uniform sampler2D noiseSampler;

uniform float xTime;
uniform float xOvercast;

vec4 PerlinPS()
{ 
	vec4 color;
    vec2 move = vec2(0,1);
    vec4 perlin = texture2D(noiseSampler, (TexCoord)+xTime*move)/2;
    perlin += texture2D(noiseSampler, (TexCoord)*2+xTime*move)/4;
    perlin += texture2D(noiseSampler, (TexCoord)*4+xTime*move)/8;
    perlin += texture2D(noiseSampler, (TexCoord)*8+xTime*move)/16;
    perlin += texture2D(noiseSampler, (TexCoord)*16+xTime*move)/32;
    perlin += texture2D(noiseSampler, (TexCoord)*32+xTime*move)/32;    
    
    color.rgb = 1-pow(perlin.r, xOvercast)*2;
    color.a =1;

    return color;
}
 

void main()
{
     gl_FragColor = PerlinPS();
}