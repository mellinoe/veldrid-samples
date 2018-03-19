#version 450 core
#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

layout(set = 0, binding = 0) uniform projView
{
    mat4 View;
    mat4 Proj;
};

layout(location = 0) in vec2 Position;
layout(location = 1) in vec4 Color;
layout(location = 2) in float xOff;

layout(location = 0) out vec4 fsin_Color;

void main()
{
    vec4 offsetPosition = vec4(Position.x + xOff, Position.y, 0, 1);
    gl_Position = Proj * View * offsetPosition;
    gl_Position.y = -gl_Position.y; // Correct for Vulkan clip coordinates

    fsin_Color = Color;
}
