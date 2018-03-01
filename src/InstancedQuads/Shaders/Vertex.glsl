#version 330 core

layout(std140) uniform projView
{
    mat4 View;
    mat4 Proj;
};

layout(location = 0)in vec2 Position;
layout(location = 1)in vec4 Color;
layout(location = 2)in float xOff;

// flat out vec4 fsin_Color; // takes colour from "dominant" vertex
smooth out vec4 fsin_Color;
// noperspective out vec4 fsin_Color; // interpolates in screen space

void main()
{
    vec2 posOff = vec2(Position.x + xOff,Position.y);
    vec4 worldPos = vec4(posOff, 5, 1);
    mat4 viewMatrix = View;
    mat4 projMatrix = Proj;
    gl_Position = projMatrix*viewMatrix*worldPos;
    fsin_Color = Color;
}
