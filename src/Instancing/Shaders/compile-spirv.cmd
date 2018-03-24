@echo off

glslangvalidator -V -S vert Instance-Vertex.450.glsl -o Instance-Vertex.450.glsl.spv
glslangvalidator -V -S frag Instance-Fragment.450.glsl -o Instance-Fragment.450.glsl.spv

glslangvalidator -V -S vert Planet-Vertex.450.glsl -o Planet-Vertex.450.glsl.spv
glslangvalidator -V -S frag Planet-Fragment.450.glsl -o Planet-Fragment.450.glsl.spv

glslangvalidator -V -S vert Starfield-Vertex.450.glsl -o Starfield-Vertex.450.glsl.spv
glslangvalidator -V -S frag Starfield-Fragment.450.glsl -o Starfield-Fragment.450.glsl.spv