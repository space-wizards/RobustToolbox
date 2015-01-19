texture background;
texture sprite;

float2 spriteDimensions;
float2 backbufferSize;

float cloakAmount <
    string UIWidget = "slider";
    float UIMin = 0.0;
    float UIMax = 1.0;
    float UIStep = 0.000125;
> = 0.0f;

float refractionIndex <
    string UIWidget = "slider";
    float UIMin = 1.0;
    float UIMax = 32.0f;
    float UIStep = 0.00000025;
> = 1.0f;

sampler backgroundSampler = sampler_state
{
    texture = <background>;
};

sampler spriteSampler = sampler_state
{
    texture = <sprite>;
};

/* data from application vertex buffer */
struct VTX_OUTPUT
{
	float4 position : POSITION;	
	float2 texCoords : TEXCOORD0;
	float4 diffuse : COLOR;
  float4 color : COLOR0;
};

/********* pixel shader ********/

float4 simplePS(VTX_OUTPUT IN) : COLOR {
  float2 scaler = spriteDimensions/backbufferSize;
  float2 backPos = float2(0,0);
  float4 spriteColor;
  float4 newColor; 

  spriteColor = tex2D(spriteSampler, IN.texCoords);
    
  if (spriteColor.a > 0.0f)
  {  	
	backPos = ((IN.texCoords) * scaler);

	if ((spriteColor.r >= 0.0f) && (spriteColor.r < 0.5f))
		backPos.x += (spriteColor.r * cloakAmount) * (scaler.x / refractionIndex);
	else
	    	backPos.x -= (spriteColor.r * cloakAmount) * (scaler.x / refractionIndex);

	if ((spriteColor.g >= 0.0f) && (spriteColor.g < 0.5f))
		backPos.y += (spriteColor.g * cloakAmount) * (scaler.y / refractionIndex);
	else
		backPos.y -= (spriteColor.g * cloakAmount) * (scaler.y / refractionIndex);

	backPos.x += (spriteColor.b * cloakAmount) * (scaler.y / refractionIndex);
	backPos.y += (spriteColor.b * cloakAmount) * (scaler.y / refractionIndex);

	newColor = tex2D(backgroundSampler, backPos);
    	newColor = (newColor * (1 - (spriteColor.a - cloakAmount))) + (spriteColor *  (spriteColor.a - cloakAmount));	
    	newColor.a = spriteColor.a;
    	newColor *= IN.diffuse;	
  }
  
  return newColor;
}

/*************/

technique main
{
	pass p0    
	{
		PixelShader = compile ps_2_0 simplePS();
	}
}

/***************************** eof ***/
