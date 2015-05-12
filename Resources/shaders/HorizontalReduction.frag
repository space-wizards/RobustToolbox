varying vec2 v_texCoord0;

uniform sampler2D inputSampler;
uniform vec2 TextureDimensions;


vec4 HorizontalReductionPS()
{
	vec2 color = texture2D(inputSampler, v_texCoord0);
	vec2 colorR = texture2D(inputSampler, v_texCoord0 + vec2(TextureDimensions.x,0));
	vec2 result = min(color, colorR);
	return vec4(result,0,1);
} 

void main()
{
	gl_FragColor = HorizontalReductionPS();

}