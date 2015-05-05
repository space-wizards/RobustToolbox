// Honk. :0) 

#define NUM_LIGHTS 6
uniform vec4 LightPosData[NUM_LIGHTS];
uniform vec4 Colors[NUM_LIGHTS];

uniform sampler2D light1s;
uniform sampler2D light2s;
uniform sampler2D light3s;
uniform sampler2D light4s;
uniform sampler2D light5s;
uniform sampler2D light6s;

uniform sampler2D sceneSampler;



varying vec4 a_texCoord0;

vec4 PreLightBlendPS(vec2 TexCoord) 
{
	vec4 l[NUM_LIGHTS];
	vec2 ltc[NUM_LIGHTS];
	for(int i = 0;i<NUM_LIGHTS;i++)
	{
		ltc[i] = vec2((TexCoord.x - LightPosData[i].x) * LightPosData[i].z, (TexCoord.y - LightPosData[i].y) * LightPosData[i].w);
	}
	l[0] = texture2D(light1s, ltc[0]);
	l[1] = texture2D(light2s, ltc[1]);
	l[2] = texture2D(light3s, ltc[2]);
	l[3] = texture2D(light4s, ltc[3]);
	l[4] = texture2D(light5s, ltc[4]);
	l[5] = texture2D(light6s, ltc[5]);
	
	l[0].rgb = l[0].rgb * Colors[0].rgb;
	l[1].rgb = l[1].rgb * Colors[1].rgb;
	l[2].rgb = l[2].rgb * Colors[2].rgb;
	l[3].rgb = l[3].rgb * Colors[3].rgb;
	l[4].rgb = l[4].rgb * Colors[4].rgb;
	l[5].rgb = l[5].rgb * Colors[5].rgb;
	
	
	vec4 s = texture2D(sceneSampler, TexCoord); // sample existing lights
	
	//Add the lights together	
	float r = sqrt(pow(l[0].r, 2) + pow(l[1].r, 2) + pow(l[2].r, 2) + pow(l[3].r, 2) + pow(l[4].r, 2) + pow(l[5].r, 2) + pow(s.r, 2));
	float g = sqrt(pow(l[0].g, 2) + pow(l[1].g, 2) + pow(l[2].g, 2) + pow(l[3].g, 2) + pow(l[4].g, 2) + pow(l[5].g, 2) + pow(s.g, 2));
	float b = sqrt(pow(l[0].b, 2) + pow(l[1].b, 2) + pow(l[2].b, 2) + pow(l[3].b, 2) + pow(l[4].b, 2) + pow(l[5].b, 2) + pow(s.b, 2));
	vec4 c = vec4(r,g,b, 1);
		
	//Return the light color
	return vec4(c.rgb,min(1, 1/max(c.r, max(c.g,c.b))))*1;
}

void main()
{
	gl_FragColor = PreLightBlendPS(a_texCoord0);
}


