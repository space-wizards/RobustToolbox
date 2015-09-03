uniform sampler2D playerViewSampler;
uniform sampler2D sceneSampler;
uniform sampler2D lightSampler;

vec4 DrawTilesInversePlayerViewPS(vec2 TexCoord)
{
	vec4 pv = texture2D(playerViewSampler, TexCoord);
	float v = 1 - min(length(pv.rgb),1);
	vec4 s = texture2D(sceneSampler, TexCoord);
	vec4 t = texture2D(lightSampler, TexCoord);
	return vec4(t.rgb + s.rgb, v);
}



void main()
{
	gl_FragColor = DrawTilesInversePlayerViewPS(gl_TexCoord[0]);
}