struct ParticleInfo
{
    float2 Position;
    float2 Velocity;
    float4 Color;
};

cbuffer ScreenSizeBuffer : register(b0)
{
    float Width;
    float Height;
    float2 Padding__;
}

RWStructuredBuffer<ParticleInfo> Particles : register(u0);

#define PARTICLE_COUNT 1024

[numthreads(1, 1, 1)]
void CS(uint3 dtid : SV_DispatchThreadID)
{
    uint index = dtid.x;
    if (index > PARTICLE_COUNT)
    {
        return;
    }

    float2 pos = Particles[index].Position;
    float2 vel = Particles[index].Velocity;

    float2 newPos = pos + vel;
    float2 newVel = vel;
    if (newPos.x > Width)
    {
        newPos.x -= (newPos.x - Width);
        newVel.x *= -1;
    }
    if (newPos.x < 0)
    {
        newPos.x *= -1;
        newVel.x *= -1;
    }
    if (newPos.y > Height)
    {
        newPos.y -= (newPos.y - Height);
        newVel.y *= -1;
    }
    if (newPos.y < 0)
    {
        newPos.y = -newPos.y;
        newVel.y *= -1;
    }

    Particles[index].Position = newPos;
    Particles[index].Velocity = newVel;
}