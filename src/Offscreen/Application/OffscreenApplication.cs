using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using AssetPrimitives;
using SampleBase;
using Veldrid;
using Veldrid.SPIRV;

namespace Offscreen
{
    public class OffscreenApplication : SampleApplication
    {
        private const uint OffscreenWidth = 1024;
        private const uint OffscreenHeight = 1024;

        private CommandList _cl;
        private Framebuffer _offscreenFB;
        private Pipeline _offscreenPipeline;
        private ProcessedModel _dragonModel;
        private uint _dragonIndexCount;
        private IndexFormat _dragonIndexFormat;
        private ProcessedModel _planeModel;
        private ProcessedMeshPart _planeMesh;
        private Texture _colorMap;
        private TextureView _colorView;
        private Texture _offscreenColor;
        private TextureView _offscreenView;
        //private VertexLayoutDescription _vertexLayout;
        private Pipeline _dragonPipeline;
        private Pipeline _mirrorPipeline;
        private Vector3 _dragonPos = new Vector3(0, 1.5f, 0);
        private Vector3 _dragonRotation = new Vector3(0, 0, 0);

        private DeviceBuffer _planeVertexBuffer;
        private DeviceBuffer _planeIndexBuffer;
        private DeviceBuffer _dragonVertexBuffer;
        private DeviceBuffer _dragonIndexBuffer;

        private DeviceBuffer _uniformBuffers_vsShared;
        private DeviceBuffer _uniformBuffers_vsMirror;
        private DeviceBuffer _uniformBuffers_vsOffScreen;
        private ResourceSet _offscreenResourceSet;
        private ResourceSet _dragonResourceSet;
        private ResourceSet _mirrorResourceSet;

        public OffscreenApplication(ApplicationWindow window) : base(window)
        {
            _camera.Position = new Vector3(0, 1, 6f);
        }

        protected override void CreateResources(ResourceFactory factory)
        {
            var vertexElements = new[]
            {
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("TextureCoordinate", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("Normal", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3)
            };
            var vertexLayout = new VertexLayoutDescription(vertexElements);

            _planeModel = LoadEmbeddedAsset<ProcessedModel>("plane2.binary");
            _planeMesh = _planeModel.MeshParts[0];
            Debug.Assert(Enumerable.SequenceEqual(_planeMesh.VertexElements, vertexElements), "Check plane mesh vertex data compatibility");

            _planeVertexBuffer = factory.CreateBuffer(new BufferDescription((uint)_planeMesh.VertexData.Length, BufferUsage.VertexBuffer));
            GraphicsDevice.UpdateBuffer(_planeVertexBuffer, 0, _planeMesh.VertexData);
            _planeIndexBuffer = factory.CreateBuffer(new BufferDescription((uint)_planeMesh.IndexData.Length, BufferUsage.IndexBuffer));
            GraphicsDevice.UpdateBuffer(_planeIndexBuffer, 0, _planeMesh.IndexData);

            _dragonModel = LoadEmbeddedAsset<ProcessedModel>("chinesedragon.binary");

            _dragonModel.MergeMeshesToSingleVertexAndIndexBuffer(out var dragonVertexData, out var dragonVertexElementDescriptions, out var dragonIndexData, out _dragonIndexCount, out _dragonIndexFormat);
            Debug.Assert(Enumerable.SequenceEqual(dragonVertexElementDescriptions, vertexElements), "Check dragon mesh vertex data compatibility");

            _dragonVertexBuffer = factory.CreateBuffer(new BufferDescription((uint)dragonVertexData.Length, BufferUsage.VertexBuffer));
            GraphicsDevice.UpdateBuffer(_dragonVertexBuffer, 0, dragonVertexData);
            _dragonIndexBuffer = factory.CreateBuffer(new BufferDescription((uint)dragonIndexData.Length, BufferUsage.IndexBuffer));
            GraphicsDevice.UpdateBuffer(_dragonIndexBuffer, 0, dragonIndexData);

            var ktxData = LoadEmbeddedAsset<byte[]>("darkmetal_bc3_unorm.binary");
            _colorMap = KtxFile.LoadTexture(
                GraphicsDevice,
                factory,
                ktxData,
                PixelFormat.BC3_UNorm);

            _colorView = factory.CreateTextureView(_colorMap);

            _offscreenColor = factory.CreateTexture(TextureDescription.Texture2D(
                OffscreenWidth, OffscreenHeight, 1, 1,
                 PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.RenderTarget | TextureUsage.Sampled));
            _offscreenView = factory.CreateTextureView(_offscreenColor);
            Texture offscreenDepth = factory.CreateTexture(TextureDescription.Texture2D(
                OffscreenWidth, OffscreenHeight, 1, 1, PixelFormat.R16_UNorm, TextureUsage.DepthStencil));
            _offscreenFB = factory.CreateFramebuffer(new FramebufferDescription(offscreenDepth, _offscreenColor));

            ShaderSetDescription phongShaders = new ShaderSetDescription(
                new[] { vertexLayout },
                factory.CreateFromSpirv(
                    new ShaderDescription(ShaderStages.Vertex, ReadEmbeddedAssetBytes("Phong-vertex.glsl"), "main"),
                    new ShaderDescription(ShaderStages.Fragment, ReadEmbeddedAssetBytes("Phong-fragment.glsl"), "main")));

            ResourceLayout phongLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("UBO", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            GraphicsPipelineDescription pd = new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.DepthOnlyLessEqual,
                new RasterizerStateDescription(FaceCullMode.Front, PolygonFillMode.Solid, FrontFace.Clockwise, true, false),
                PrimitiveTopology.TriangleList,
                phongShaders,
                phongLayout,
                _offscreenFB.OutputDescription);
            _offscreenPipeline = factory.CreateGraphicsPipeline(pd);

            pd.Outputs = GraphicsDevice.SwapchainFramebuffer.OutputDescription;
            pd.RasterizerState = RasterizerStateDescription.Default;
            _dragonPipeline = factory.CreateGraphicsPipeline(pd);

            ResourceLayout mirrorLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("UBO", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("ReflectionMap", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("ReflectionMapSampler", ResourceKind.Sampler, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("ColorMap", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("ColorMapSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            ShaderSetDescription mirrorShaders = new ShaderSetDescription(
                new[] { vertexLayout },
                factory.CreateFromSpirv(
                    new ShaderDescription(ShaderStages.Vertex, ReadEmbeddedAssetBytes("Mirror-vertex.glsl"), "main"),
                    new ShaderDescription(ShaderStages.Fragment, ReadEmbeddedAssetBytes("Mirror-fragment.glsl"), "main")));

            GraphicsPipelineDescription mirrorPD = new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.DepthOnlyLessEqual,
                new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, false),
                PrimitiveTopology.TriangleList,
                mirrorShaders,
                mirrorLayout,
                GraphicsDevice.SwapchainFramebuffer.OutputDescription);
            _mirrorPipeline = factory.CreateGraphicsPipeline(ref mirrorPD);

            _uniformBuffers_vsShared = factory.CreateBuffer(new BufferDescription(208, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            _uniformBuffers_vsMirror = factory.CreateBuffer(new BufferDescription(208, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            _uniformBuffers_vsOffScreen = factory.CreateBuffer(new BufferDescription(208, BufferUsage.UniformBuffer | BufferUsage.Dynamic));

            _offscreenResourceSet = factory.CreateResourceSet(new ResourceSetDescription(phongLayout, _uniformBuffers_vsOffScreen));
            _dragonResourceSet = factory.CreateResourceSet(new ResourceSetDescription(phongLayout, _uniformBuffers_vsShared));
            _mirrorResourceSet = factory.CreateResourceSet(new ResourceSetDescription(mirrorLayout,
                _uniformBuffers_vsMirror,
                _offscreenView,
                GraphicsDevice.LinearSampler,
                _colorView,
                GraphicsDevice.Aniso4xSampler));

            _cl = factory.CreateCommandList();
        }

        public struct UniformInfo
        {
            public Matrix4x4 Projection;
            public Matrix4x4 View;
            public Matrix4x4 Model;
            public Vector4 LightPos;
        }

        protected override void Draw(float deltaSeconds)
        {
            _dragonRotation.Y += deltaSeconds * 10f;

            UpdateUniformBuffers();
            UpdateUniformBufferOffscreen();

            _cl.Begin();
            DrawOffscreen();
            DrawMain();
            _cl.End();
            GraphicsDevice.SubmitCommands(_cl);
            GraphicsDevice.SwapBuffers();
        }

        private void DrawOffscreen()
        {
            _cl.SetFramebuffer(_offscreenFB);
            _cl.SetFullViewports();
            _cl.ClearColorTarget(0, RgbaFloat.Black);
            _cl.ClearDepthStencil(1f);

            _cl.SetPipeline(_offscreenPipeline);
            _cl.SetGraphicsResourceSet(0, _offscreenResourceSet);
            _cl.SetVertexBuffer(0, _dragonVertexBuffer);
            _cl.SetIndexBuffer(_dragonIndexBuffer, _dragonIndexFormat);
            _cl.DrawIndexed(_dragonIndexCount, 1, 0, 0, 0);
        }

        private void DrawMain()
        {
            _cl.SetFramebuffer(GraphicsDevice.SwapchainFramebuffer);
            _cl.SetFullViewports();
            _cl.ClearColorTarget(0, RgbaFloat.Black);
            _cl.ClearDepthStencil(1f);

            _cl.SetPipeline(_mirrorPipeline);
            _cl.SetGraphicsResourceSet(0, _mirrorResourceSet);
            _cl.SetVertexBuffer(0, _planeVertexBuffer);
            _cl.SetIndexBuffer(_planeIndexBuffer, _planeMesh.IndexFormat);
            _cl.DrawIndexed(_planeMesh.IndexCount, 1, 0, 0, 0);

            _cl.SetPipeline(_dragonPipeline);
            _cl.SetGraphicsResourceSet(0, _dragonResourceSet);
            _cl.SetVertexBuffer(0, _dragonVertexBuffer);
            _cl.SetIndexBuffer(_dragonIndexBuffer, _dragonIndexFormat);
            _cl.DrawIndexed(_dragonIndexCount, 1, 0, 0, 0);
        }

        public static float DegreesToRadians(float degrees)
        {
            return degrees * (float)Math.PI / 180f;
        }

        private void UpdateUniformBuffers()
        {
            UniformInfo ui = new UniformInfo { LightPos = new Vector4(0, 0, 0, 1) };

            ui.Projection = Matrix4x4.CreatePerspectiveFieldOfView(DegreesToRadians(60.0f), Window.Width / (float)Window.Height, 0.1f, 256.0f);

            ui.View = Matrix4x4.CreateLookAt(_camera.Position, _camera.Position + _camera.Forward, Vector3.UnitY);

            ui.Model = Matrix4x4.CreateRotationX(DegreesToRadians(_dragonRotation.X));
            ui.Model = Matrix4x4.CreateRotationY(DegreesToRadians(_dragonRotation.Y)) * ui.Model;
            ui.Model = Matrix4x4.CreateTranslation(_dragonPos) * ui.Model;

            GraphicsDevice.UpdateBuffer(_uniformBuffers_vsShared, 0, ref ui);

            // Mirror
            ui.Model = Matrix4x4.Identity;
            GraphicsDevice.UpdateBuffer(_uniformBuffers_vsMirror, 0, ref ui);
        }

        private void UpdateUniformBufferOffscreen()
        {
            UniformInfo ui = new UniformInfo { LightPos = new Vector4(0, 0, 0, 1) };

            ui.Projection = Matrix4x4.CreatePerspectiveFieldOfView(DegreesToRadians(60.0f), Window.Width / (float)Window.Height, 0.1f, 256.0f);

            ui.View = Matrix4x4.CreateLookAt(_camera.Position, _camera.Position + _camera.Forward, Vector3.UnitY);

            ui.Model = Matrix4x4.CreateRotationX(DegreesToRadians(_dragonRotation.X));
            ui.Model = Matrix4x4.CreateRotationY(DegreesToRadians(_dragonRotation.Y)) * ui.Model;
            ui.Model = Matrix4x4.CreateScale(new Vector3(1, -1, 1)) * ui.Model;
            ui.Model = Matrix4x4.CreateTranslation(_dragonPos) * ui.Model;

            GraphicsDevice.UpdateBuffer(_uniformBuffers_vsOffScreen, 0, ref ui);
        }
    }
}
