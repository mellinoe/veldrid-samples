#version 450 core
// Physically Based Rendering
// Copyright (c) 2017-2018 Michał Siejak

// Environment skybox: Fragment program.

layout(location=0) in vec3 localPosition;
layout(location=0) out vec4 color;

#if VULKAN
layout(set=0, binding=1) uniform textureCube envTexture;
layout(set=0, binding=2) uniform sampler textureSampler;

vec4 textureLod(textureCube tex, vec3 p, float lod) {
	return textureLod(samplerCube(tex, textureSampler), p, lod);
}
#else
layout(binding=0) uniform samplerCube envTexture;
#endif // VULKAN

void main()
{
	vec3 envVector = normalize(localPosition);
	color = textureLod(envTexture, envVector, 0);
}
