#include <metal_stdlib>
using namespace metal;

#define HASHSCALE3 float3(443.897, 441.423, 437.195)
#define STARFREQUENCY 0.01

struct InvCameraInfo
{
    float4x4 InvProj;
    float4x4 InvView;
};

struct FragmentIn
{
    float4 Position[[position]];
    float4 ClipPos;
    float3 TexCoord;
};

// Hash function by Dave Hoskins (https://www.shadertoy.com/view/4djSRW)
float hash33(float3 p3)
{
    p3 = fract(p3 * HASHSCALE3);
    p3 += dot(p3, p3.yxz + float3(19.19, 19.19, 19.19));
    return fract((p3.x + p3.y) * p3.z + (p3.x + p3.z) * p3.y + (p3.y + p3.z) * p3.x);
}

float3 starField(float3 pos)
{
    float3 color = float3(0, 0, 0);
    float threshhold = (1.0 - STARFREQUENCY);
    float rnd = hash33(pos);
    if (rnd >= threshhold)
    {
        float starCol = pow(abs((rnd - threshhold) / (1.0 - threshhold)), 16.0);
        color += float3(starCol, starCol, starCol);
    }
    return color;
}

fragment float4 FS(
    FragmentIn input[[stage_in]],
    constant InvCameraInfo& camInfo[[buffer(0)]])
{
    // View Coordinates
    float4 viewCoords = camInfo.InvProj * input.ClipPos;
    viewCoords.z = -1.0f;
    viewCoords.w = 0.0f;

    float3 worldDirection = (camInfo.InvView * viewCoords).xyz;
    worldDirection = normalize(worldDirection);

    worldDirection = floor(worldDirection * 700) / 700;

    return float4(starField(worldDirection), 1.0);
}
