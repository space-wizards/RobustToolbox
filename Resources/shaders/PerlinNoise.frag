
uniform sampler2D noiseSampler;

uniform float xTime;
uniform float xOvercast;

vec4 PerlinPS()
{ 
	vec4 color;
    vec2 move = vec2(0,1);

    vec4 perlin = texture2D(noiseSampler, (gl_TexCoord[0])+ mul(xTime,move)/2);
    perlin += texture2D(noiseSampler, mul((gl_TexCoord[0]),2)+mul(xTime,move)/4);
    perlin += texture2D(noiseSampler, mul((gl_TexCoord[0]),4)+mul(xTime,move)/8);
    perlin += texture2D(noiseSampler, mul((gl_TexCoord[0]),8)+mul(xTime,move)/16);
    perlin += texture2D(noiseSampler, mul((gl_TexCoord[0]),16)+mul(xTime,move)/32);
    perlin += texture2D(noiseSampler, mul((gl_TexCoord[0]),32)+mul(xTime,move)/32);    
    
    color.rgb = mul(1-pow(perlin.r, xOvercast),2);

    color.a =1;

    return color;
}
 

void main()
{
     gl_FragColor = PerlinPS();
}
