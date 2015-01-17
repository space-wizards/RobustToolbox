Texture _spriteImage;

// Amount to blur.
float blurAmount 
<	
    string units = "inches";
    string UIWidget = "slider";
    float uimin = 0.00001;
    float uimax = 0.11125;
    float uistep = 0.00001;
    string UIName = "Blur";
> = 0.0135;

// Our texture sampler.
sampler2D sourceSampler = sampler_state 
{ 
	texture = <_spriteImage>;
};

// Our processed vertex.
struct VTX_OUTPUT
{
	float4 position : POSITION;
	float4 diffuse : COLOR0;
	float2 texCoords : TEXCOORD0;
};

// Function to perform the sampling for the blur.
float4 psBlurSample(float2 Tex : TEXCOORD0, float4 baseColor, float offX, float offY) : COLOR0
{
	float4 Color;				// Output.
  	float scaler = 0;			// Scale of the sample.
  	  
  	// Calculate sample.
  	scaler = (1 + (offY * offX));
	Tex.x = Tex.x + offX;
	Tex.y = Tex.y + offY;
	
   	Color = baseColor + tex2D(sourceSampler, Tex / scaler);   	
   	return Color;
}

// Function to blur an image.
float4 psBlur(VTX_OUTPUT vtx) : COLOR0
{
  	float4 Color = 0;				// Output.
  	float Alpha = 0;				// Alpha component.
  	float blurValue = 0;				// Blur value.
  	
  	blurValue = blurAmount / 1000.0f;
  	
  	if (blurAmount < 0.0f)
  	   blurValue = 0;
  		
  	if (blurAmount > 10.0f)
  	   blurValue = 0.01;

	Color = tex2D(sourceSampler, vtx.texCoords);	
	// Store the alpha for later, we don't want to blur that.
	Alpha = Color.a;
	
	// Sample eight directions + the center.
  	Color = psBlurSample(vtx.texCoords, Color, -blurValue, -blurValue);
  	Color = psBlurSample(vtx.texCoords, Color, 0, -blurValue);
  	Color = psBlurSample(vtx.texCoords, Color, blurValue, -blurValue);  	
  	Color = psBlurSample(vtx.texCoords, Color, -blurValue, blurValue);
  	Color = psBlurSample(vtx.texCoords, Color, 0, blurValue);
  	Color = psBlurSample(vtx.texCoords, Color, blurValue, blurValue);  	
  	Color = psBlurSample(vtx.texCoords, Color, -blurValue, 0);
  	Color = psBlurSample(vtx.texCoords, Color, blurValue, 0);
  	
  	// Calculate final color.
   	Color.rgb = saturate((Color.rgb / 9) * vtx.diffuse.rgb);
   	// Restore and combine the alpha.
   	Color.a = Alpha * vtx.diffuse.a;
   	
    return Color;
}

// Technique to blur an image.
technique Blur
{	
	pass p1
	{
		VertexShader = null;
		PixelShader = compile ps_2_0 psBlur();
	}
}