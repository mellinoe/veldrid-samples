#include <metal_stdlib>
using namespace metal;

struct PixelInput
{
    float4 Position[[position]];
    float2 TexCoords;
};

fragment float4 FS(
    PixelInput input[[stage_in]],
    texture2d<float> Tex[[texture(0)]],
    texture2d<float> Tex11[[texture(1)]],
    texture2d<float> Tex22[[texture(2)]],
    sampler SS[[sampler(0)]])
{
        return Tex.sample(SS, input.TexCoords) + Tex11.sample(SS, input.TexCoords) * 0.01 + Tex22.sample(SS, input.TexCoords) * 0.01;
}