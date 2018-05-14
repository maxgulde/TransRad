/* Shader for thermal analysis
 
 Provides techniques for the drawing of colored and wireframe triangle based models
 BasicColorDrawing: Model color drawing without shading / lighting; Can display darkened model if in Earth shadow
 Wireframe: Fast wireframe drawing method based on barycentric coordinates
 
 Author: Max Gulde
 Last Update: 2018-05-09
*/

#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Parameters
const float p_ShadingIntensity = 0.6;
const float p_WireFrameThickness = 0.01;
const float p_WireFrameFaceAlpha = 0.1;
const float p_WireFrameBrightness = 1.0;

// Extern / uniform variables
matrix WorldViewProjection;
float LightIntensity;

struct VertexShaderInput
{
	float4 Position : SV_POSITION;
	float3 Barycentric : NORMAL;
	float4 Color : COLOR0;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float3 Barycentric : NORMAL;
	float4 Color : COLOR0;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	output.Position = mul(input.Position, WorldViewProjection);
	output.Color = input.Color;
	output.Color.a = 1.0;
	output.Barycentric = input.Barycentric;

	return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	return input.Color * (LightIntensity * p_ShadingIntensity + (1 - p_ShadingIntensity));
}

float4 MainPS_Wireframe(VertexShaderOutput input) : COLOR
{
	float4 C = input.Color;

	// Parts close to edge are drawn opaque, otherwise transparent
	if (any(input.Barycentric < p_WireFrameThickness))
	{
		C = float4(p_WireFrameBrightness, p_WireFrameBrightness, p_WireFrameBrightness, 1.0);
	}
	else
	{
		C = float4(0.0, 0.0, 0.0, p_WireFrameFaceAlpha);
	}

	return C;
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

technique Wireframe
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS_Wireframe();

		AlphaBlendEnable = true;
		DestBlend = INVSRCALPHA;
		SrcBlend = SRCALPHA;
		CullMode = None;
		ZEnable = true;
	}
};


