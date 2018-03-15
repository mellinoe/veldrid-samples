@echo off

glslangvalidator -V -S vert Instance-vertex.450.glsl -o Instance-vertex.450.glsl.spv
glslangvalidator -V -S frag Instance-fragment.450.glsl -o Instance-fragment.450.glsl.spv

glslangvalidator -V -S vert Planet-vertex.450.glsl -o Planet-vertex.450.glsl.spv
glslangvalidator -V -S frag Planet-fragment.450.glsl -o Planet-fragment.450.glsl.spv

glslangvalidator -V -S vert Starfield-vertex.450.glsl -o Starfield-vertex.450.glsl.spv
glslangvalidator -V -S frag Starfield-fragment.450.glsl -o Starfield-fragment.450.glsl.spv