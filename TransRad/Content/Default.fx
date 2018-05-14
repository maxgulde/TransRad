/* Shader for thermal analysis
 
 Provides techniques for the drawing of colored models
 BasicColorDrawing: Model color drawing without shading / lighting; Can display darkened model if in Earth shadow
 
 Author: Max Gulde
 Last Update: 2018-05-14
*/

#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Extern / uniform variables
matrix WorldViewProjection;
int MeshNumber;
int SourceIdx;
int TargetIdx;

struct VertexShaderInput
{
	float4 Position : SV_POSITION;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	output.Position = mul(input.Position, WorldViewProjection);
	output.Color = float4(0.5, 0.5, 0.5, 1);

	if (MeshNumber == SourceIdx)
	{
		output.Color = float4(0.8, 0, 0, 1);
	}
	if (MeshNumber == TargetIdx)
	{
		output.Color = float4(0, 0.8, 0, 1);
	}

	return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	return input.Color;
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();

		AlphaBlendEnable = false;
		CullMode = None;
		ZEnable = true;
	}
};


