#version 330 core

layout(std140) uniform projView
{
    mat4 View;
    mat4 Proj;
};

in vec2 Position;
in vec4 Color;
in float xOff;

smooth out vec4 fsin_Color;

void main()
{
    vec4 offsetPosition = vec4(Position.x + xOff, Position.y, 0, 1);
    gl_Position = Proj * View * offsetPosition;

    fsin_Color = Color;
}
