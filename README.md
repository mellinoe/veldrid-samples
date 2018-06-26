# Veldrid Samples

This repository contains sample projects for [Veldrid](https://github.com/mellinoe/veldrid), a low-level graphics and compute library for .NET Many of the samples are intended to demonstrate how specific features in the library can be used.

## Running the Samples

To build and run the samples, the .NET Core SDK is required. All of the sample projects can be run on .NET Core on any supported platform. Simply run:

`dotnet run -p <project-file>`

Some of the samples can also be run on iOS and Android. Veldrid.Samples.Mobile.sln is a solution file containing all of the mobile projects. Veldrid.Samples.sln contains only the desktop projects.

## [Getting Started](src/GettingStarted)

This is a simple introduction to Veldrid, which renders a simple 4-color quad in the center of the screen.

Check out the [Tutorial Page](https://mellinoe.github.io/veldrid-docs/articles/getting-started/intro.html) for a step-by-step walkthrough of this project, with an explanation of each portion.

![GettingStarted](https://i.imgur.com/6QY0CZb.png?2)

## [Textured Cube](src/TexturedCube/Application)

This sample renders a textured 3D Cube, which rotates in the center of the screen.

![Textured Cube](https://i.imgur.com/SOrGqj8.png?1)

## [Instancing](src/Instancing/Application)

This sample renders a large number of instanced 3D meshes, orbiting around a central textured sphere.

![Instancing](https://i.imgur.com/BP3raIg.png?1)

## [Offscreen](src/Offscreen/Application)

This sample renders planar reflection using a sampled offscreen Framebuffer.

![Offscreen](https://i.imgur.com/UJthGTA.png?1)

## [Animated Mesh](src/AnimatedMesh/Application)

This sample renders an animated mesh.

![Animated Mesh](https://i.imgur.com/LKUbLPm.png?1)

## [Compute Particles](src/ComputeParticles/Application)

This sample simulates a large number of particles in a compute shader. Afterwards, it renders their positions using instanced points.

![Compute Particles](https://i.imgur.com/S8bm6xs.png)

## [Compute Texture](src/ComputeTexture/Application)

This sample procedurally generates texture data in a compute shader every frame. Afterwards, it renders the texture data as a full-screen quad.

![Compute Texture](https://i.imgur.com/1yGwzTU.png?1)
