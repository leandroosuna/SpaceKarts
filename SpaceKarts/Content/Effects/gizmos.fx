#define VS_SHADERMODEL vs_5_0
#define PS_SHADERMODEL ps_5_0

float4x4 WorldViewProjection;
float3 Color;

struct VertexShaderInput
{
    float4 Position : POSITION;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float3 Color : TEXCOORD0;
};

VertexShaderOutput MainVertexShader(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    
    // Project position
    output.Position = mul(input.Position, WorldViewProjection);

    
    return output;
}

float4 MainPixelShader(VertexShaderOutput input) : COLOR
{
    return float4(Color, 1.0);
}

float4 BackgroundPixelShader(VertexShaderOutput input) : COLOR
{
    return float4(Color * 0.5, 1.0);
}


technique Gizmos
{
    pass Background
    {
        VertexShader = compile VS_SHADERMODEL MainVertexShader();
        PixelShader = compile PS_SHADERMODEL BackgroundPixelShader();
    }
    pass Foreground
    {
        VertexShader = compile VS_SHADERMODEL MainVertexShader();
        PixelShader = compile PS_SHADERMODEL MainPixelShader();
    }
};
