#version 450 core
// Physically Based Rendering
// Copyright (c) 2017-2018 Michał Siejak

// Physically Based shading model: Vertex program.

layout(location=0) in vec3 position;
layout(location=1) in vec3 normal;
layout(location=2) in vec3 tangent;
layout(location=3) in vec3 bitangent;
layout(location=4) in vec2 texcoord;

#if VULKAN
layout(set=0, binding=0) uniform TransformUniforms
#else
layout(std140, binding=0) uniform TransformUniforms
#endif // VULKAN
{
	mat4 viewProjectionMatrix;
	mat4 skyProjectionMatrix;
	mat4 sceneRotationMatrix;
};

layout(location=0)
out vec3 vout_pos;
layout(location=1)
out vec2 vout_texcoord;
layout(location=2)
out mat3 vout_tangentBasis;

void main()
{
	vout_pos = vec3(sceneRotationMatrix * vec4(position, 1.0));
	vout_texcoord = vec2(texcoord.x, 1.0-texcoord.y);

	// Pass tangent space basis vectors (for normal mapping).
	vout_tangentBasis = mat3(sceneRotationMatrix) * mat3(tangent, bitangent, normal);

	gl_Position = viewProjectionMatrix * sceneRotationMatrix * vec4(position, 1.0);
}
