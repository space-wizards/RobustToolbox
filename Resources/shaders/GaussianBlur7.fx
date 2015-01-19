float4x4 _projectionMatrix;

// Vertex shader input structure
struct VS_INPUT
{
    float4 Position   : POSITION;
    float2 Texture    : TEXCOORD0;
};


// Vertex shader output structure
struct VS_OUTPUT
{
    float4 Position   : POSITION;
    float2 Texture    : TEXCOORD0;
};

VS_OUTPUT vs_main( VS_INPUT In )
{
    VS_OUTPUT Out;                      //create an output vertex

	Out.Position = float4(mul(In.Position, _projectionMatrix).xyz, 1.0f);
    Out.Texture  = In.Texture;          //copy original texcoords

    return Out;                         //return output vertex
}

#define RADIUS 7
#define KERNEL_SIZE (RADIUS * 2 + 1)

//-----------------------------------------------------------------------------
// Globals.
//-----------------------------------------------------------------------------

float2 weights_offsets[KERNEL_SIZE];

//-----------------------------------------------------------------------------
// Textures.
//-----------------------------------------------------------------------------

texture colorMapTexture;

sampler2D colorMap = sampler_state
{
    Texture = <colorMapTexture>;
    MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};

//-----------------------------------------------------------------------------
// Pixel Shaders.
//-----------------------------------------------------------------------------

float4 PS_GaussianBlurHorizontal(float2 texCoord : TEXCOORD) : COLOR0
{
    float4 color = float4(0.0f, 0.0f, 0.0f, 0.0f);
    
    for (int i = 0; i < KERNEL_SIZE; ++i)
        color += tex2D(colorMap, float2(texCoord.x + weights_offsets[i].y, texCoord.y)) * weights_offsets[i].x;
        
    return color;
}

float4 PS_GaussianBlurVertical(float2 texCoord : TEXCOORD) : COLOR0
{
    float4 color = float4(0.0f, 0.0f, 0.0f, 0.0f);
    
    for (int i = 0; i < KERNEL_SIZE; ++i)
        color += tex2D(colorMap, float2(texCoord.x, texCoord.y + weights_offsets[i].y)) * weights_offsets[i].x;
        
    return color;
}



technique GaussianBlurHorizontal
{
	pass P0
	{
		AlphaBlendEnable = true;
		VertexShader = compile vs_3_0 vs_main();
		PixelShader = compile ps_3_0 PS_GaussianBlurHorizontal();
	}
}

technique GaussianBlurVertical
{
	pass P0
	{
		AlphaBlendEnable = true;
		VertexShader = compile vs_3_0 vs_main();
		PixelShader = compile ps_3_0 PS_GaussianBlurVertical();
	}
}