varying vec2 v_texCoord;

uniform sampler2D maskSampler;
uniform sampler2D lightSampler;

uniform vec4 MaskProps;
uniform vec4 DiffuseColor;

vec4 MaskLightPS(vec2 TexCoord)
{
	vec4 p = MaskProps;
	vec2 tc = TexCoord;
	vec4 d = DiffuseColor;
	float t;
	if(p.x > 0) // x is rot 90 degrees
	{ // We just flip the axes.
		t = tc.x;
		tc.x = tc.y;
		tc.y = t;
	}
	if(p.y > 0) // y is flip horizontally
	{
		tc.x = 1 - tc.x;
	}
	if(p.z > 0) // z is flip vertically
	{
		tc.y = 1 - tc.y;
	}
	
	vec4 l = texture2D(lightSampler, TexCoord);
	l = vec4(l.r * d.r, l.g * d.g, l.b * d.b, l.a); 
	vec4 m = texture2D(maskSampler, tc);
	
	return vec4(l.rgb, m.r)* l.a;
}

void main()
{
	gl_FragColor = MaskLightPS(v_texCoord);

}
