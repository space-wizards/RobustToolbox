#version 120
uniform vec4 MaskProps;
uniform vec4 DiffuseColor;
uniform vec2 renderTargetSize;
uniform float AttenuateShadows;

uniform sampler2D inputSampler;
uniform sampler2D shadowMapSampler;


varying vec2 TexCoord;

float GetShadowDistanceH(vec2 TexCoord,float displacementV)
{
  	float u = TexCoord.x;
	float v = TexCoord.y;

	u = abs(u-0.5) * 2;
	v = v * 2 - 1;
	float v0 = v/u;
	v0+=displacementV;
	v0 = (v0 + 1) / 2;

	vec2 newCoords = vec2(TexCoord.x,v0);
	//horizontal info was stored in the Red component
	return texture2D(shadowMapSampler, newCoords).r;
}

float GetShadowDistanceV(vec2 TexCoord, float displacementV)
{
	float u = TexCoord.y;
	float v = TexCoord.x;

	u = abs(u-0.5) * 2;
	v = v * 2 - 1;
	float v0 = v/u;
	v0+=displacementV;
	v0 = (v0 + 1) / 2;

	vec2 newCoords = vec2(TexCoord.y,v0);
	//vertical info was stored in the Green component
	return texture2D(shadowMapSampler, newCoords).g;
}

float DrawShadowsPS()
{
	// distance of this pixel from the center
	float Distance = length(gl_TexCoord[0].xy - 0.5);
	//Distance *= renderTargetSize.x;
	//apply a 2-pixel bias
	//Distance -=2;

	//distance stored in the shadow map
	float shadowMapDistance;

	//coords in [-1,1]
	float nY = 2.0*( gl_TexCoord[0].y - 0.5);
	float nX = 2.0*( gl_TexCoord[0].x - 0.5);

	//we use these to determine which quadrant we are in
	if(abs(nY)<abs(nX))
	{
		shadowMapDistance = GetShadowDistanceH(gl_TexCoord[0].xy,0);
	}
	else
	{
		shadowMapDistance = GetShadowDistanceV(gl_TexCoord[0].xy,0);
	}

	//if distance to this pixel is lower than distance from shadowMap,
	//then we are not in shadow
	float light = Distance <= shadowMapDistance ? 1:0;

	float d = 2 * length(gl_TexCoord[0].xy - 0.5);
	float attenuation = max(pow(clamp(1 - d, 0,1),1), AttenuateShadows); //If AttenuateShadows is true, attenuation

	return 1 - (light * attenuation) / 2;
}

void main()
{
	gl_FragColor = vec4(0, 0, 0, DrawShadowsPS());
}
