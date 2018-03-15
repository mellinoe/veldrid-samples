@echo off

glslangvalidator -S vert Vertex.430.glsl
glslangvalidator -S frag Fragment.430.glsl
glslangvalidator -S comp Compute.430.glsl