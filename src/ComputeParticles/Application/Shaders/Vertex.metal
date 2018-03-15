#include <metal_stdlib>
using namespace metal;

struct ParticleInfo
{
    packed_float2 Position;
    packed_float2 Velocity;
    packed_float4 Color;
};

struct ScreenSizeInfo
{
    float Width;
    float Height;
    packed_float2 Padding__;
};

struct PixelInput
{
    float4 Position[[position]];
    float PointSize[[point_size]];
    float4 Color;
};

vertex PixelInput VS(
    uint vertexID[[vertex_id]],
    device ParticleInfo *Particles[[buffer(0)]],
    constant ScreenSizeInfo &screenSize[[buffer(1)]])
{
    ParticleInfo input = Particles[vertexID];
    PixelInput output;
    float4 outPos = float4(input.Position / float2(screenSize.Width, screenSize.Height), 0, 1);
    outPos.xy = 2 * ((outPos.xy - float2(0.5, 0.5)));
    output.Position = outPos;
    output.Color = input.Color;
    output.PointSize = 1;
    return output;
}
