#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

layout(set = 0, binding = 1) uniform ScreenSizeBuffer
{
    float ScreenWidth;
    float ScreenHeight;
    vec2 Padding_;
};

layout(set = 0, binding = 0, rgba32f) uniform image2D Tex;

layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

void main()
{
    imageStore(Tex, ivec2(gl_GlobalInvocationID.xy), vec4(gl_GlobalInvocationID.x / ScreenWidth, gl_GlobalInvocationID.y / ScreenHeight, 0, 1));
}
