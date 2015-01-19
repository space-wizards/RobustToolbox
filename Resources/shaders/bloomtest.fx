Texture _spriteImage;

sampler2D postTex = sampler_state 
{ 
	texture = <_spriteImage>;
};

float Luminance = 0.063f;
static const float fMiddleGray = 0.275f;  //Higher = Affects more.
static const float fWhiteCutoff = 0.75f; //Lower = Stuff whites-out more easily

struct VTX_OUTPUT
{
	float4 position : POSITION;
	float4 diffuse : COLOR0;
	float2 texCoords : TEXCOORD0;
};

#define OFFSET 0.00225
#define MULT 1.40

float2 Offsets[9] =
{
	{ -OFFSET, -OFFSET },
	{ OFFSET, OFFSET },
	{  0.000,  0.000 },
	{ -OFFSET, OFFSET },
	{ OFFSET, -OFFSET },
	{ 0, -OFFSET }, //Down
	{ 0, OFFSET },  //Up
	{ -OFFSET, 0 }, //Left
	{ OFFSET, 0 },  //Right
};

float Weights[9] =
{
	0.120985,
	0.120985,
	0.220985,
	0.120985,
	0.120985,
	0.120985,
	0.120985,
	0.120985,
	0.120985,
};

float4 Bloom1(VTX_OUTPUT vtx) : COLOR0
{
	float3 pixel;
	float3 Color = 0;
	
	for(int i = 0; i < 5; i++)
	{
		pixel = tex2D(postTex,vtx.texCoords + Offsets[i]);
		
		pixel *= fMiddleGray / (Luminance + 0.001f);
		pixel *= (1.0f + (pixel / (fWhiteCutoff * fWhiteCutoff)));
		pixel -= 5.0f;
		
		pixel = max(pixel,0.0f);
		pixel /= (10.0f + pixel);
		
		Color += pixel * Weights[i];
	}
	
	Color *= MULT;
	
	return float4(Color,1.0) + tex2D(postTex,vtx.texCoords);
}

float4 Bloom2(VTX_OUTPUT vtx) : COLOR0
{
	float3 pixel;
	float3 Color = 0;
	
	for(int i = 5; i < 9; i++)
	{
		pixel = tex2D(postTex,vtx.texCoords + Offsets[i]);
		
		pixel *= fMiddleGray / (Luminance + 0.001f);
		pixel *= (1.0f + (pixel / (fWhiteCutoff * fWhiteCutoff)));
		pixel -= 5.0f;
		
		pixel = max(pixel,0.0f);
		pixel /= (10.0f + pixel);
		
		Color += pixel * Weights[i];
	}
	
	Color *= MULT;
		
	return float4(Color,1.0) + tex2D(postTex,vtx.texCoords);
}


technique bloomtest
{
	pass p1
	{
		VertexShader = null;
		PixelShader = compile ps_2_0 Bloom1();
	}
	
	pass p2
	{
		VertexShader = null;
		PixelShader = compile ps_2_0 Bloom2();
	}
}