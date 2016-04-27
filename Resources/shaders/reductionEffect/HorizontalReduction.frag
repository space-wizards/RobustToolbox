#version 120
uniform sampler2D texture;
uniform float TextureDimensions;


vec4 HorizontalReductionPS()
{
	vec2 color = texture2D(texture, gl_TexCoord[0].xy).rg;	
	vec2 colorR;
	// This modulus bullshit is because every texel in the source image is evaluated in GLSL
	// If we don't check whether the texel is odd or even, we would end up
	// incorrectly taking the max of the pixel to the right versus this one,
	// making our horizontal reduction invalid.
	if (mod(gl_TexCoord[0].x / TextureDimensions, 2) == 0) 
	    colorR = texture2D(texture, gl_TexCoord[0].xy + vec2(TextureDimensions,0)).rg;
	else
		colorR = texture2D(texture, gl_TexCoord[0].xy - vec2(TextureDimensions,0)).rg;
	vec2 result = min(color, colorR);
	return vec4(result,0,1);
} 

void main()
{
	gl_FragColor = HorizontalReductionPS();

}
