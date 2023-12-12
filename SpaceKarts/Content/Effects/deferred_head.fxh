
float4x4 world;
float4x4 view;
float4x4 projection;

float4x4 prevView;
float4x4 prevProjection;

float2 screenSize;
float radius;



struct PVSI
{
    float4 Position : POSITION;
    float2 TexCoord : TEXCOORD;
};
struct PVSO
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD;
    float4 Clip : TEXCOORD2;
};
struct PSO
{
    float4 color : COLOR0;
    float4 blurH : COLOR1;
    float4 blurV : COLOR2;
};

texture colorMap;
sampler colorSampler = sampler_state
{
    Texture = (colorMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    Mipfilter = LINEAR;
};
texture normalMap;
sampler normalMapSampler = sampler_state
{
    Texture = (normalMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    Mipfilter = LINEAR;
};

texture lightMap;
sampler lightMapSampler = sampler_state
{
    Texture = (lightMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    Mipfilter = LINEAR;
};

