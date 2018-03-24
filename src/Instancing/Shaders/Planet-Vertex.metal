#include <metal_stdlib>
using namespace metal;

struct ProjView
{
    float4x4 View;
    float4x4 Proj;
};

struct VertexIn
{
    float3 Position[[attribute(0)]];
    float3 Normal[[attribute(1)]];
    float2 TexCoord[[attribute(2)]];
};

struct FragmentIn
{
    float4 Position[[position]];
    float3 Position_WorldSpace[[attribute(0)]];
    float3 Normal[[attribute(1)]];
    float2 TexCoord[[attribute(2)]];
};

vertex FragmentIn VS(
    VertexIn input[[stage_in]],
    constant ProjView& projView[[buffer(1)]])
{
    FragmentIn output;
    float4 pos = float4(input.Position, 1);
    output.Position_WorldSpace = input.Position;
    output.Position = projView.Proj * projView.View * pos;
    output.Normal = input.Normal;
    output.TexCoord = input.TexCoord * float2(10, 6);
    return output;
}
