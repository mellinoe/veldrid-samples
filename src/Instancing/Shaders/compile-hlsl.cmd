@echo off

fxc /E VS /T vs_5_0 Instance-Vertex.hlsl /Fo Instance-Vertex.hlsl.bytes
fxc /E FS /T ps_5_0 Instance-Fragment.hlsl /Fo Instance-Fragment.hlsl.bytes

fxc /E VS /T vs_5_0 Planet-Vertex.hlsl /Fo Planet-Vertex.hlsl.bytes
fxc /E FS /T ps_5_0 Planet-Fragment.hlsl /Fo Planet-Fragment.hlsl.bytes

fxc /E VS /T vs_5_0 Starfield-Vertex.hlsl /Fo Starfield-Vertex.hlsl.bytes
fxc /E FS /T ps_5_0 Starfield-Fragment.hlsl /Fo Starfield-Fragment.hlsl.bytes