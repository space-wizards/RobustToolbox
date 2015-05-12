varying vec2 TexCoord;

uniform sampler2D sceneSampler;


float duration;
vec3 color_offset = vec3(0.2, 0.4, 0.3);
int Iterations = 128;
vec2 Pan = vec2(0.3776610, -0.3435075);
float Zoom = 0.4;
float Aspect = 1;
vec2 JuliaSeed = vec2(-0.439, 0.576);
vec3 ColorScale = vec3(6, 5, 4);

vec4 DeathShaderPS()
{
	vec4 c = texture2D(sceneSampler, TexCoord);
	c.bg = 0;
	return c;
}

void main()
{
	gl_FragColor = DeathShaderPS();
}