uniform sampler2D LightTexture;
uniform sampler2D SceneTexture;
uniform sampler2D PlayerViewTexture;
uniform sampler2D OutOfViewTexture;

uniform vec4 AmbientLight;
uniform vec4 MaskProps;


vec4 LightBlendPS(vec2 TexCoord)
{
	vec4 a = AmbientLight;
	vec4 l = texture2D(LightTexture, TexCoord); //Sample light/shadows
	l = max(l,a); // Set a minimum level of light
	vec4 c = texture2D(SceneTexture, TexCoord); //Sample scene color
	vec4 pv = texture2D(PlayerViewTexture, TexCoord); // Sample player view
	
	vec2 masktc = TexCoord;
	masktc.x = masktc.x * MaskProps.x * MaskProps.z;
	masktc.y = masktc.y * MaskProps.y * MaskProps.w;
	
	vec4 t = texture2D(OutOfViewTexture, masktc); // Sample mask
	
	//Generate scuzz for occluded areas
	/*float4 t;	
	float2 lines;
	lines.x = 1 * (TexCoord.x * MaskProps.x + MaskProps.z);
	lines.y = 1 * (TexCoord.y * MaskProps.y + MaskProps.w);
	float s = (sin(lines.x + lines.y + lines.y) + sin(lines.x - lines.y));
	t.rgb = 0;
	if(s > 0.5)
	{
		t.rgb = 0.1;
	}*/
	
	t.a = 1;
	//End generate scuzz
	
	vec3 h; // calculate hard light
	h.r = l.r <= 0.5 ? 2 * l.r * c.r : 1 - (2 * (1 - l.r) * (1 - c.r) );
	h.g = l.g <= 0.5 ? 2 * l.g * c.g : 1 - (2 * (1 - l.g) * (1 - c.g) );
	h.b = l.b <= 0.5 ? 2 * l.b * c.b : 1 - (2 * (1 - l.b) * (1 - c.b) );

	vec4 result;
	result = vec4(max(c.rgb*l.rgb, h.rgb), pv.r)* 1;
	result = result + (t *( 1 - pv.r));
	return result;
}


void main()
{

 gl_FragColor = LightBlendPS(gl_TexCoord[0]);

}
