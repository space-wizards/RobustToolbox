float duration;
float3 color_offset = float3(0.2, 0.4, 0.3);

texture SceneTexture;
sampler sceneSampler = sampler_state
{
	Texture = <SceneTexture>;
	
	MipFilter = Point;
	MinFilter = Point;
	MagFilter = Point;
	
	AddressU = Clamp;
	AddressV = Clamp;
};

int Iterations = 128;
float2 Pan = float2(0.3776610, -0.3435075);
float Zoom = 0.4;
float Aspect = 1;
float2 JuliaSeed = float2(-0.439, 0.576);
float3 ColorScale = float3(6, 5, 4);

float ComputeValue(float2 v, float2 offset)
{
	float vxsquare = 0;
	float vysquare = 0;

	int iteration = 0;
	int lastIteration = Iterations;

	do
	{
		vxsquare = v.x * v.x;
		vysquare = v.y * v.y;

		v = float2(vxsquare - vysquare, v.x * v.y * 2) + offset;

		iteration++;

		if ((lastIteration == Iterations) && (vxsquare + vysquare) > 4.0)
		{
			lastIteration = iteration + 1;
		}
	}
	while (iteration < lastIteration);

	return (float(iteration) - (log(log(sqrt(vxsquare + vysquare))) / log(2.0))) / float(Iterations);
}

float4 Mandelbrot_PixelShader(float2 texCoord, float zoom)
{
	float2 v = (texCoord - 0.5) * zoom * float2(1, Aspect) - Pan;

	float val = ComputeValue(v, v);

	return float4(sin(val * ColorScale.x), sin(val * ColorScale.y), sin(val * ColorScale.z), 1);
}

float4 Julia_PixelShader(float2 texCoord : TEXCOORD0, float zoom, float2 seed, float2 pan, float3 colorscale) : COLOR0
{
	float2 v = (texCoord - 0.5) * zoom * float2(1, Aspect) - pan;
	float val = ComputeValue(v, seed);

	return float4(sin(val * colorscale.x), sin(val * colorscale.y), sin(val * colorscale.z), 1);
}
float4 HardLight(float4 c, float4 l)
{
	float3 h;
	h.r = l.r <= 0.5 ? 2 * l.r * c.r : 1 - (2 * (1 - l.r) * (1 - c.r) );
	h.g = l.g <= 0.5 ? 2 * l.g * c.g : 1 - (2 * (1 - l.g) * (1 - c.g) );
	h.b = l.b <= 0.5 ? 2 * l.b * c.b : 1 - (2 * (1 - l.b) * (1 - c.b) );
    return float4(h,1);
}
float4 DeathShaderPS(float2 TexCoord : TEXCOORD0) : COLOR0
{
	float4 c = tex2D(sceneSampler, TexCoord);
	c.bg = 0;
	return c;
}


/*float4 DeathShaderPS(float2 TexCoord : TEXCOORD0) : COLOR0
{
	float4 c = tex2D(sceneSampler, TexCoord);
	//float3 offset = color_offset + duration;
	float2 tc1 = TexCoord;
	float2 tc2 = TexCoord;
	float cd = cos(duration);
	float sd = sin(duration);
	tc1.x = tc1.x * cd - tc1.y * sd;
	tc1.y = tc1.x * sd + tc1.y * cd;
	tc2.x = tc2.x * sd - tc2.y * cd;
	tc2.y = tc2.x * cd + tc2.y * sd;
	tc1 = mul(tc1, 10);
	tc2 = mul(tc2, 8);
	tc1.x = tc1.x * (cd * 0.5 + 0.5);
	tc1.y = tc1.y * (sd * 0.5 + 0.5);
	tc2.x = tc2.x * (sd * 0.5 + 0.5);
	tc2.y = tc2.y * (cd * 0.5 + 0.5);
	
    float3 l;
    l.x = 0.25 * sin(tc1.x * tc2.y) + 0.25 * sin(tc2.x * tc1.y) + 0.5;
	l.y = 0.25 * sin((1 - tc2.x) * tc1.y) + 0.25 * sin((1 - tc1.x) * tc2.y) + 0.5;
	l.z = 0.25 * sin(tc1.x * (1 - tc1.y)) + 0.25 * sin(tc2.x * (1 - tc2.y)) + 0.5;
 
	float3 h;
	h.r = l.r <= 0.5 ? 2 * l.r * c.r : 1 - (2 * (1 - l.r) * (1 - c.r) );
	h.g = l.g <= 0.5 ? 2 * l.g * c.g : 1 - (2 * (1 - l.g) * (1 - c.g) );
	h.b = l.b <= 0.5 ? 2 * l.b * c.b : 1 - (2 * (1 - l.b) * (1 - c.b) );
    return float4(h,1);
}*/
/*
float4 DeathShaderPS(float2 TexCoord : TEXCOORD0) : COLOR0
{
	float4 color = tex2D(sceneSampler, TexCoord);
	float d = sin(duration);
	float3 l = float3(d * TexCoord.x * 10, 15 * d / TexCoord.y, 20 * d * TexCoord.y / TexCoord.x);
	return float4(color.r * (sin(l.x) * 0.5 + 0.5), color.g * (cos(l.y) * 0.5 + 0.5), color.b * (sin(l.z) * 0.5 + 0.5), 1);
	//return color;
}*/


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

technique DeathShader
{
	pass P0
	{
		VertexShader = compile vs_3_0 vs_main();
		PixelShader = compile ps_3_0 DeathShaderPS();
	}
}
