#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

struct ParticleInfo
{
    vec2 Position;
    vec2 Velocity;
    vec4 Color;
};

layout(std140, set = 0, binding = 0) readonly buffer ParticlesBuffer
{
    ParticleInfo Particles[];
};

layout(set = 1, binding = 0) uniform ScreenSizeBuffer
{
    float ScreenWidth;
    float ScreenHeight;
    vec2 Padding_;
};

layout (location = 0) out vec4 Color;

void main()
{
    ParticleInfo pi = Particles[gl_VertexIndex];
    gl_Position = vec4(pi.Position / vec2(ScreenWidth, ScreenHeight), 0, 1);
    gl_Position.xy = 2 * (gl_Position.xy - vec2(0.5, 0.5));
    Color = pi.Color;
}
