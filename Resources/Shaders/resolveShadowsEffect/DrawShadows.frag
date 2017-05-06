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

vec4 MaskLight(vec4 inColor, vec2 TexCoord)
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
	
	vec4 l = inColor;
	l = vec4(l.r * d.r, l.g * d.g, l.b * d.b, l.a); 
	vec4 m = texture2D(inputSampler, tc);
	
	return vec4((l.rgb * m.r), l.a);
}

vec4 DrawShadowsPS()
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
	  
	  vec4 result = vec4(1 - (light * attenuation) / 2);
	  result = MaskLight(result, gl_TexCoord[0].xy);
	  return result;
}

void main()
{
	gl_FragColor = DrawShadowsPS();
}