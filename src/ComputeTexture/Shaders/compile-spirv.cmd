@echo off

glslangvalidator -V -S vert Vertex.450.glsl -o Vertex.spv
glslangvalidator -V -S frag Fragment.450.glsl -o Fragment.spv
glslangvalidator -V -S comp Compute.450.glsl -o Compute.spv