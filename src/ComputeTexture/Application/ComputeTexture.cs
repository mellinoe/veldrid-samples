using SampleBase;
using System;
using System.IO;
using System.Numerics;
using Veldrid;

namespace ComputeTexture
{
    public class ComputeTexture : SampleApplication
    {
        private DeviceBuffer _screenSizeBuffer;
        private DeviceBuffer _shiftBuffer;
        private DeviceBuffer _vertexBuffer;
        private DeviceBuffer _indexBuffer;
        private Shader _computeShader;
        private ResourceLayout _computeLayout;
        private Pipeline _computePipeline;
        private ResourceSet _computeResourceSet;
        private Shader _vertexShader;
        private Shader _fragmentShader;
        private Pipeline _graphicsPipeline;
        private ResourceSet _graphicsResourceSet;
        private CommandList _cl;

        private Texture _computeTargetTexture;
        private TextureView _computeTargetTextureView;
        private ResourceLayout _graphicsLayout;
        private float _ticks;
        private bool _initialized;

        public ComputeTexture(ApplicationWindow window) : base(window) { }

        protected override void CreateResources(ResourceFactory factory)
        {
            _screenSizeBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _shiftBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            _vertexBuffer = factory.CreateBuffer(new BufferDescription(16 * 4, BufferUsage.VertexBuffer));
            _indexBuffer = factory.CreateBuffer(new BufferDescription(2 * 6, BufferUsage.IndexBuffer));

            _computeShader = factory.CreateShader(new ShaderDescription(
                ShaderStages.Compute,
                ReadEmbeddedAssetBytes($"Compute.{GetExtension(factory.BackendType)}"),
                "CS"));

            _computeLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Tex", ResourceKind.TextureReadWrite, ShaderStages.Compute),
                new ResourceLayoutElementDescription("ScreenSizeBuffer", ResourceKind.UniformBuffer, ShaderStages.Compute),
                new ResourceLayoutElementDescription("ShiftBuffer", ResourceKind.UniformBuffer, ShaderStages.Compute)));

            ComputePipelineDescription computePipelineDesc = new ComputePipelineDescription(
                _computeShader,
                _computeLayout,
                1, 1, 1);
            _computePipeline = factory.CreateComputePipeline(ref computePipelineDesc);

            _vertexShader = factory.CreateShader(new ShaderDescription(
                ShaderStages.Vertex,
                ReadEmbeddedAssetBytes($"Vertex.{GetExtension(factory.BackendType)}"),
                "VS"));
            _fragmentShader = factory.CreateShader(new ShaderDescription(
                ShaderStages.Fragment,
                ReadEmbeddedAssetBytes($"Fragment.{GetExtension(factory.BackendType)}"),
                "FS"));

            ShaderSetDescription shaderSet = new ShaderSetDescription(
                new VertexLayoutDescription[]
                {
                    new VertexLayoutDescription(
                        new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float2),
                        new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2))
                },
                new[]
                {
                    _vertexShader,
                    _fragmentShader
                });

            _graphicsLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Tex", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Tex11", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Tex22", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("SS", ResourceKind.Sampler, ShaderStages.Fragment)));

            GraphicsPipelineDescription fullScreenQuadDesc = new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.Disabled,
                new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.Clockwise, false, false),
                PrimitiveTopology.TriangleList,
                shaderSet,
                new[] { _graphicsLayout },
                MainSwapchain.Framebuffer.OutputDescription);

            _graphicsPipeline = factory.CreateGraphicsPipeline(ref fullScreenQuadDesc);

            _cl = factory.CreateCommandList();

            CreateWindowSizedResources(factory);
            InitResources(factory);
            _initialized = true;
        }

        private void CreateWindowSizedResources(ResourceFactory factory)
        {
            _computeTargetTexture?.Dispose();
            _computeTargetTextureView?.Dispose();
            _computeResourceSet?.Dispose();
            _graphicsResourceSet?.Dispose();

            _computeTargetTexture = factory.CreateTexture(TextureDescription.Texture2D(
                Window.Width,
                Window.Height,
                1,
                1,
                PixelFormat.R32_G32_B32_A32_Float,
                TextureUsage.Sampled | TextureUsage.Storage));

            _computeTargetTextureView = factory.CreateTextureView(_computeTargetTexture);

            _computeResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                _computeLayout,
                _computeTargetTextureView,
                _screenSizeBuffer,
                _shiftBuffer));

            _graphicsResourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                _graphicsLayout,
                _computeTargetTextureView,
                _computeTargetTextureView,
                _computeTargetTextureView,
                GraphicsDevice.PointSampler));
        }

        private string GetExtension(GraphicsBackend backendType)
        {
            return backendType == GraphicsBackend.Direct3D11 ? "hlsl.bytes"
                : backendType == GraphicsBackend.Vulkan ? "spv"
                    : backendType == GraphicsBackend.Metal ? "metal"
                        : backendType == GraphicsBackend.OpenGL ? "430.glsl"
                            : "300.glsles";
        }

        private void InitResources(ResourceFactory factory)
        {
            _cl.Begin();
            _cl.UpdateBuffer(_screenSizeBuffer, 0, new Vector4(Window.Width, Window.Height, 0, 0));

            Vector4[] quadVerts =
            {
                new Vector4(-1, 1, 0, 0),
                new Vector4(1, 1, 1, 0),
                new Vector4(1, -1, 1, 1),
                new Vector4(-1, -1, 0, 1),
            };

            ushort[] indices = { 0, 1, 2, 0, 2, 3 };

            _cl.UpdateBuffer(_vertexBuffer, 0, quadVerts);
            _cl.UpdateBuffer(_indexBuffer, 0, indices);

            _cl.End();
            GraphicsDevice.SubmitCommands(_cl);
            GraphicsDevice.WaitForIdle();
        }

        protected override void HandleWindowResize()
        {
            GraphicsDevice.UpdateBuffer(_screenSizeBuffer, 0, new Vector4(Window.Width, Window.Height, 0, 0));
            CreateWindowSizedResources(ResourceFactory);
        }

        protected override void Draw(float deltaSeconds)
        {
            if (!_initialized) { return; }

            _cl.Begin();
            _ticks += deltaSeconds * 1000f;
            Vector4 shifts = new Vector4(
                Window.Width * (float)Math.Cos(_ticks / 500f), // Red shift
                Window.Height * (float)Math.Sin(_ticks / 1250f), // Green shift
                (float)Math.Sin(_ticks / 1000f), // Blue shift
                0); // Padding
            _cl.UpdateBuffer(_shiftBuffer, 0, ref shifts);

            _cl.SetPipeline(_computePipeline);
            _cl.SetComputeResourceSet(0, _computeResourceSet);
            _cl.Dispatch((uint)Window.Width, (uint)Window.Height, 1);

            _cl.SetFramebuffer(MainSwapchain.Framebuffer);
            _cl.SetFullViewports();
            _cl.SetFullScissorRects();
            _cl.ClearColorTarget(0, RgbaFloat.Black);
            _cl.SetPipeline(_graphicsPipeline);
            _cl.SetVertexBuffer(0, _vertexBuffer);
            _cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            _cl.SetGraphicsResourceSet(0, _graphicsResourceSet);
            _cl.DrawIndexed(6, 1, 0, 0, 0);

            _cl.End();
            GraphicsDevice.SubmitCommands(_cl);
            GraphicsDevice.SwapBuffers(MainSwapchain);
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
