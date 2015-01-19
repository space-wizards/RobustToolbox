Texture _spriteImage;

sampler2D sourceSampler = sampler_state 
{ 
	texture = <_spriteImage>;
};

struct VTX_OUTPUT
{
	float4 position : POSITION;
	float4 diffuse : COLOR0;
	float2 texCoords : TEXCOORD0;
};

float4 psBlack(VTX_OUTPUT vtx) : COLOR0
{	
	float4 ColorSample = tex2D(sourceSampler, vtx.texCoords);
	float4 Color = 0;
	
	if(ColorSample.a > 0.0)
	{
		Color.a = 1.0;
	}
	
    return Color;
}

technique Black
{	
	pass p1
	{
		VertexShader = null;
		PixelShader = compile ps_2_0 psBlack();
	}
}