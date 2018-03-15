#version 430

struct ParticleInfo
{
    vec2 Position;
    vec2 Velocity;
    vec4 Color;
};

layout(std140) readonly buffer ParticlesBuffer
{
    ParticleInfo Particles[];
};

uniform ScreenSizeBuffer
{
    float ScreenWidth;
    float ScreenHeight;
    vec2 Padding_;
};

out vec4 Color;

void main()
{
    ParticleInfo pi = Particles[gl_VertexID];
    gl_Position = vec4(pi.Position / vec2(ScreenWidth, ScreenHeight), 0, 1);
    gl_Position.xy = 2 * (gl_Position.xy - vec2(0.5, 0.5));
    Color = pi.Color;
}
