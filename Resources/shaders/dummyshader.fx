
float4 DummyShader(float4 color : COLOR0) : COLOR0
{
	return color;
}

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

technique Mandelbrot
{
	pass P0
	{
		VertexShader = compile vs_3_0 vs_main();
		//PixelShader = compile ps_3_0 DummyShader();
	}
}
