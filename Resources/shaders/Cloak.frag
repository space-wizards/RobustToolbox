
uniform sampler2D backgroundSampler;
uniform sampler2D spriteSampler;

vec2 spriteDimensions;
vec2 backbufferSize;

float cloakAmount  = 0;
  


float refractionIndex = 1;



vec4 simplePS()
{
  vec2 scaler = spriteDimensions/backbufferSize;
  vec2 backPos = vec2(0,0);
  vec4 spriteColor;
  vec4 newColor; 

  spriteColor = texture2D(spriteSampler, gl_TexCoord[0]);
    
  if (spriteColor.a > 0)
  {  	
	backPos = ((gl_TexCoord[0]) * scaler);

	if ((spriteColor.r >= 0) && (spriteColor.r < 0.5))
		backPos.x += (spriteColor.r * cloakAmount) * (scaler.x / refractionIndex);
	else
	    	backPos.x -= (spriteColor.r * cloakAmount) * (scaler.x / refractionIndex);

	if ((spriteColor.g >= 0.0) && (spriteColor.g < 0.5))
		backPos.y += (spriteColor.g * cloakAmount) * (scaler.y / refractionIndex);
	else
		backPos.y -= (spriteColor.g * cloakAmount) * (scaler.y / refractionIndex);

	backPos.x += (spriteColor.b * cloakAmount) * (scaler.y / refractionIndex);
	backPos.y += (spriteColor.b * cloakAmount) * (scaler.y / refractionIndex);

	newColor = texture2D(backgroundSampler, backPos);
    	newColor = (newColor * (1 - (spriteColor.a - cloakAmount))) + (spriteColor *  (spriteColor.a - cloakAmount));	
    	newColor.a = spriteColor.a;
    	newColor *= vec4(.8,.8,.8,1);	
  }
  
  return newColor;
}


void main()
{
	gl_FragColor = simplePS();
}