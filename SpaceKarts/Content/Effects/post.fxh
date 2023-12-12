texture bloomFilter;
sampler bloomFilterSampler = sampler_state
{
    Texture = (bloomFilter);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = POINT;
    MinFilter = POINT;
    Mipfilter = POINT;
};
texture blurH;
sampler2D blurHSampler = sampler_state
{
    Texture = (blurH);
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};
texture blurV;
sampler2D blurVSampler = sampler_state
{
    Texture = (blurV);
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

texture positionMap;
sampler positionMapSampler = sampler_state
{
    Texture = (positionMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    Mipfilter = LINEAR;
};
texture prevPositionMap;
sampler prevPositionMapSampler = sampler_state
{
    Texture = (prevPositionMap);
    AddressU = CLAMP;
    AddressV = CLAMP;
    MagFilter = LINEAR;
    MinFilter = LINEAR;
    Mipfilter = LINEAR;
};

float4 processBloom(float3 color, float filter, float3 light, float2 texCoord)
{
    float3 blurHColor = tex2D(blurHSampler, texCoord).rgb;
    float3 blurVColor = tex2D(blurVSampler, texCoord).rgb;
    
    float attenuation = 0.8;
    float lightPower = .7;
    
    if (filter == 0)
        return float4(color * attenuation + blurHColor * lightPower + blurVColor * lightPower, 1);
    else
        return float4(color * attenuation * light + blurHColor * lightPower + blurVColor * lightPower, 1);
    
    
}
int motionBlurIntensity;
bool bloomPassBefore;
float4 processMotionBlur(sampler2D colorSampler, float3 light, float2 texCoord, float4x4 view, float4x4 projection)
{
    float4 colorRaw = tex2D(colorSampler, texCoord);
    float3 color = colorRaw.rgb;
    float filter = colorRaw.a;
    float3 position = tex2D(positionMapSampler, texCoord).xyz;
    float3 prevPosition = tex2D(prevPositionMapSampler, texCoord).xyz;
    
    float4 screenPos = mul(mul(float4(position, 1), view), projection);
    float2 screenPos2D = screenPos.xy / screenPos.z;
    float4 prevScreenPos = mul(mul(float4(prevPosition, 1), view), projection);
    float2 prevScreenPos2D = prevScreenPos.xy / prevScreenPos.z;
    
    float2 velocity = (screenPos2D - prevScreenPos2D);
    velocity.y = -velocity.y;

    texCoord -= velocity * 2;
    
    for (int i = 1; i < motionBlurIntensity + 2; ++i, texCoord += velocity)
    {
        float3 currentColor = tex2D(colorSampler, texCoord).rgb;
        color += currentColor;
    }
    float3 finalColor = (color / (motionBlurIntensity + 2));

    if(!bloomPassBefore)
    {
        if (filter != 0)
            finalColor *= light;
    }
    
    return float4(finalColor, 1);
}