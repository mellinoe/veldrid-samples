#include <metal_stdlib>
using namespace metal;

#define PARTICLE_COUNT 1024

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

kernel void CS(
    uint3 dtid[[thread_position_in_grid]],
    device ParticleInfo *particles[[buffer(0)]],
    constant ScreenSizeInfo &screenSize[[buffer(1)]])
{
    uint index = dtid.x;
    if (index > PARTICLE_COUNT)
    {
        return;
    }

    float2 pos = particles[index].Position;
    float2 vel = particles[index].Velocity;

    float2 newPos = pos + vel;
    float2 newVel = vel;
    if (newPos.x > screenSize.Width)
    {
        newPos.x -= (newPos.x - screenSize.Width);
        newVel.x *= -1;
    }
    if (newPos.x < 0)
    {
        newPos.x *= -1;
        newVel.x *= -1;
    }
    if (newPos.y > screenSize.Height)
    {
        newPos.y -= (newPos.y - screenSize.Height);
        newVel.y *= -1;
    }
    if (newPos.y < 0)
    {
        newPos.y = -newPos.y;
        newVel.y *= -1;
    }

    particles[index].Position = newPos;
    particles[index].Velocity = newVel;
}
