struct ParticleInfo
{
    float2 Position : Position0;
    float2 Velocity : Position1;
    float4 Color : Color0;
};

cbuffer ScreenSizeBuffer : register(b0)
{
    float ScreenWidth;
    float ScreenHeight;
    float2 Padding__;
}

StructuredBuffer<ParticleInfo> Particles : register(t0);

struct PixelInput
{
    float4 Position : SV_Position;
    float4 Color : Color0;
};

PixelInput VS(uint vertexID : SV_VertexID)
{
    ParticleInfo input = Particles[vertexID];
    PixelInput output;
    output.Position = float4(input.Position / float2(ScreenWidth, ScreenHeight), 0, 1);
    output.Position.xy = 2 * ((output.Position.xy - float2(0.5, 0.5)));
    output.Color = input.Color;
    return output;
}