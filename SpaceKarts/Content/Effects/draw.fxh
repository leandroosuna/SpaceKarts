
float4 drawPoint(float2 tex, float2 p, float3 color)
{
    if (length(tex - p) <= 1)
        return float4(color, 1);
    
    return float4(0, 0, 0, 0);

}

float4 drawLine(float2 p3, float2 p1, float2 p2, float3 color, float lineWidth)
{
    float2 p12 = p2 - p1;
    float2 p13 = p3 - p1;
    float d = dot(p12, p13) / length(p12);
    
    float2 p4 = p1 + normalize(p12) * d;
    
    if (length(p4 - p3) < lineWidth && length(p4 - p1) <= length(p12) && length(p4 - p2) <= length(p12))
        return float4(color, 1);
    
    return float4(0, 0, 0, 0);
}

float4 drawCrosshair(float2 normTex, float2 screenCenter, float crosshairLength, float crosshairThickness, float crosshairOffset, float3 crosshairColor )
{
    float4 output = float4(0, 0, 0, 0);
    float2 p1, p2;
    
    p1 = screenCenter - float2(0, crosshairOffset);
    p2 = p1 - float2(0, crosshairLength);
    output += drawLine(normTex, p1, p2, crosshairColor, crosshairThickness);
    
    p1 = screenCenter + float2(crosshairOffset, 0);
    p2 = p1 + float2(crosshairLength, 0);
    output += drawLine(normTex, p1, p2, crosshairColor, crosshairThickness);
    
    p1 = screenCenter + float2(0, crosshairOffset);
    p2 = p1 + float2(0, crosshairLength);
    output += drawLine(normTex, p1, p2, crosshairColor, crosshairThickness);
    
    p1 = screenCenter - float2(crosshairOffset, 0);
    p2 = p1 - float2(crosshairLength, 0);
    output += drawLine(normTex, p1, p2, crosshairColor, crosshairThickness);
    
    return output;
}
bool showProgressBar;
float progressBarValue;

float4 drawProgressBar(float2 normTex, float2 screenCenter)
{
    float4 output = float4(0, 0, 0, 0);
    if (showProgressBar == false)
        return output;
    float scaledVal = progressBarValue * 156 - 78;
    float2 p1, p2, p3, p4, p5, p6;
    p1 = screenCenter + float2(-80, 50);
    p2 = screenCenter + float2(80, 50);
    p3 = screenCenter + float2(-80, 60);
    p4 = screenCenter + float2(80, 60);
    p5 = screenCenter + float2(-78, 55);
    p6 = screenCenter + float2(scaledVal, 55);
    
    output += drawLine(normTex, p1, p2, float3(1, 1, 1), 1);
    output += drawLine(normTex, p3, p4, float3(1, 1, 1), 1);
    output += drawLine(normTex, p1, p3, float3(1, 1, 1), 1);
    output += drawLine(normTex, p2, p4, float3(1, 1, 1), 1);
    output += drawLine(normTex, p5, p6, float3(0, 1, 1), 3);
    
    
    return output;
}

/*
float Remap(float value, float from1, float to1, float from2, float to2)
{
    return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
}
#define FRAME_TIME_MAX 200
float frameTime[FRAME_TIME_MAX];

float4 drawFrameGraph(float2 normTex, float2 screenSize, float width, float height, float3 color)
{
    float4 output = float4(0, 0, 0, 0);
    
    float4 drawn;
    for (int i = 0; i < FRAME_TIME_MAX; i++)
    {
        float2 framePoint;
        framePoint.x = screenSize.x - width + i;
        framePoint.y =  Remap(frameTime[i], 0.005, 0.0166, 0, height);
        drawn = drawPoint(normTex, framePoint, color);
        if (drawn.a == 1)
            break;
    }
    if(drawn.a == 1)
        return drawn;
    
    return output;
}*/