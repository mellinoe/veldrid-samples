@echo off

fxc /E VS /T vs_5_0 Instance-vertex.hlsl /Fo Instance-vertex.hlsl.bytes
fxc /E FS /T ps_5_0 Instance-fragment.hlsl /Fo Instance-fragment.hlsl.bytes

fxc /E VS /T vs_5_0 Planet-vertex.hlsl /Fo Planet-vertex.hlsl.bytes
fxc /E FS /T ps_5_0 Planet-fragment.hlsl /Fo Planet-fragment.hlsl.bytes

fxc /E VS /T vs_5_0 Starfield-vertex.hlsl /Fo Starfield-vertex.hlsl.bytes
fxc /E FS /T ps_5_0 Starfield-fragment.hlsl /Fo Starfield-fragment.hlsl.bytes