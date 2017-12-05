using SampleBase;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid;

namespace ComputeParticles
{
    internal class ComputeParticles : SampleApplication
    {
        public const int ParticleCount = 1024;

        private Veldrid.Buffer _particleBuffer;
        private Veldrid.Buffer _screenSizeBuffer;
        private Shader _computeShader;
        private Pipeline _computePipeline;
        private Shader _vertexShader;
        private Shader _fragmentShader;
        private Pipeline _graphicsPipeline;
        private ResourceSet _graphicsParticleResourceSet;
        private CommandList _cl;
        private ResourceSet _screenSizeResourceSet;
        private ResourceSet _computeScreenSizeResourceSet;
        private ResourceSet _computeResourceSet;
        private bool _windowResized;

        protected override void OnWindowResized()
        {
            _windowResized = true;
        }

        protected override void CreateResources(ResourceFactory factory)
        {
            _particleBuffer = factory.CreateBuffer(
                new BufferDescription(
                    (uint)Unsafe.SizeOf<ParticleInfo>() * ParticleCount,
                    BufferUsage.StructuredBufferReadWrite,
                    (uint)Unsafe.SizeOf<ParticleInfo>()));

            _screenSizeBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));

            _computeShader = factory.CreateShader(new ShaderDescription(
                ShaderStages.Compute,
                File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Shaders", $"Compute.{GetExtension(factory.BackendType)}"))));

            ResourceLayout particleStorageLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("ParticlesBuffer", ResourceKind.StructuredBufferReadWrite, ShaderStages.Compute)));

            ResourceLayout screenSizeLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("ScreenSizeBuffer", ResourceKind.UniformBuffer, ShaderStages.Compute)));

            ComputePipelineDescription computePipelineDesc = new ComputePipelineDescription(
                new ShaderStageDescription(ShaderStages.Compute, _computeShader, "CS"),
                new[] { particleStorageLayout, screenSizeLayout });
            _computePipeline = factory.CreateComputePipeline(ref computePipelineDesc);

            _computeResourceSet = factory.CreateResourceSet(new ResourceSetDescription(particleStorageLayout, _particleBuffer));

            _computeScreenSizeResourceSet = factory.CreateResourceSet(new ResourceSetDescription(screenSizeLayout, _screenSizeBuffer));

            _vertexShader = factory.CreateShader(new ShaderDescription(
                ShaderStages.Vertex,
                File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Shaders", $"Vertex.{GetExtension(factory.BackendType)}"))));
            _fragmentShader = factory.CreateShader(new ShaderDescription(
                ShaderStages.Fragment,
                File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Shaders", $"Fragment.{GetExtension(factory.BackendType)}"))));

            ShaderSetDescription shaderSet = new ShaderSetDescription(
                Array.Empty<VertexLayoutDescription>(),
                new[]
                {
                    new ShaderStageDescription(ShaderStages.Vertex, _vertexShader, "VS"),
                    new ShaderStageDescription(ShaderStages.Fragment, _fragmentShader, "FS")
                });

            particleStorageLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("ParticlesBuffer", ResourceKind.StructuredBufferReadOnly, ShaderStages.Vertex)));

            screenSizeLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("ScreenSizeBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            GraphicsPipelineDescription particleDrawPipelineDesc = new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.Default,
                PrimitiveTopology.PointList,
                shaderSet,
                new[] { particleStorageLayout, screenSizeLayout },
                _gd.SwapchainFramebuffer.OutputDescription);

            _graphicsPipeline = factory.CreateGraphicsPipeline(ref particleDrawPipelineDesc);

            _graphicsParticleResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                particleStorageLayout,
                _particleBuffer));

            _screenSizeResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                screenSizeLayout,
                _screenSizeBuffer));

            _cl = factory.CreateCommandList();

            InitResources(factory);
        }

        private object GetExtension(GraphicsBackend backendType)
        {
            return backendType == GraphicsBackend.Direct3D11 ? "hlsl.bytes"
                : backendType == GraphicsBackend.Vulkan ? "spv"
                    : "430.glsl";
        }

        private void InitResources(ResourceFactory factory)
        {
            _cl.Begin();
            _cl.UpdateBuffer(_screenSizeBuffer, 0, new Vector4(_window.Width, _window.Height, 0, 0));

            ParticleInfo[] initialParticles = new ParticleInfo[ParticleCount];
            Random r = new Random();
            for (int i = 0; i < ParticleCount; i++)
            {
                ParticleInfo pi = new ParticleInfo();
                pi.Position = new Vector2((float)(r.NextDouble() * _window.Width), (float)(r.NextDouble() * _window.Height));
                pi.Velocity = new Vector2((float)(r.NextDouble() * 3), (float)(r.NextDouble() * 3));
                pi.Color = new Vector4(0.4f + (float)r.NextDouble() * .6f, 0.4f + (float)r.NextDouble() * .6f, 0.4f + (float)r.NextDouble() * .6f, 1);
                initialParticles[i] = pi;
            }
            _cl.UpdateBuffer(_particleBuffer, 0, initialParticles);

            _cl.End();
            _gd.ExecuteCommands(_cl);
            _gd.WaitForIdle();
        }

        protected override void Draw()
        {
            _cl.Begin();
            if (_windowResized)
            {
                _windowResized = false;
                _gd.ResizeMainWindow((uint)_window.Width, (uint)_window.Height);
                _cl.UpdateBuffer(_screenSizeBuffer, 0, new Vector4(_window.Width, _window.Height, 0, 0));
            }

            _cl.SetPipeline(_computePipeline);
            _cl.SetComputeResourceSet(0, _computeResourceSet);
            _cl.SetComputeResourceSet(1, _computeScreenSizeResourceSet);
            _cl.Dispatch(1024, 1, 1);

            _cl.SetFramebuffer(_gd.SwapchainFramebuffer);
            _cl.SetFullViewports();
            _cl.SetFullScissorRects();
            _cl.ClearColorTarget(0, RgbaFloat.Black);
            _cl.SetPipeline(_graphicsPipeline);
            _cl.SetGraphicsResourceSet(0, _graphicsParticleResourceSet);
            _cl.SetGraphicsResourceSet(1, _screenSizeResourceSet);
            _cl.Draw(ParticleCount, 1, 0, 0);
            _cl.End();

            _gd.ExecuteCommands(_cl);
            _gd.SwapBuffers();
        }
    }

    struct ParticleInfo
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector4 Color;
        public ParticleInfo(Vector2 position, Vector2 velocity, Vector4 color)
        {
            Position = position;
            Velocity = velocity;
            Color = color;
        }
    }
}
