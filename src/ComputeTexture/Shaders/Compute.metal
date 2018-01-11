#include <metal_stdlib>
using namespace metal;

#define PARTICLE_COUNT 1024

struct ScreenSizeInfo
{
    float Width;
    float Height;
    packed_float2 Padding__;
};

struct ShiftBuffer
{
    float RShift;
    float GShift;
    float BShift;
    float Padding1__;
};

kernel void CS(
    uint3 dtid[[thread_position_in_grid]],
    constant ScreenSizeInfo &screenSize[[buffer(0)]],
    constant ShiftBuffer &shiftBuffer[[buffer(1)]],
    texture2d<float, access::write> tex[[texture(0)]])
{
    float x = (dtid.x + shiftBuffer.RShift);
    float y = (dtid.y + shiftBuffer.GShift);
    tex.write(float4(x / screenSize.Width, y / screenSize.Height, shiftBuffer.BShift, 1), dtid.xy);
}
