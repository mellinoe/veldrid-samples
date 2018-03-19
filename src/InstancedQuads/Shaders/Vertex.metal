#include <metal_stdlib>
using namespace metal;

struct VertexInput
{
    float2 Position[[attribute(0)]];
    float4 Color[[attribute(1)]];
    float xOff[[attribute(2)]];
};

struct PixelInput
{
    float4 Position[[position]];
    float4 Color;
};

struct projView 
{
    float4x4 View;
    float4x4 Proj;
};

vertex PixelInput VS(VertexInput input[[stage_in]],constant projView &pj [[ buffer(2) ]])
{
    PixelInput output;

    float4 offsetPosition = float4(input.Position.x + input.xOff, input.Position.y, 0, 1);
    output.Position = pj.Proj * pj.View * offsetPosition;

    output.Color = input.Color;
    return output;
}