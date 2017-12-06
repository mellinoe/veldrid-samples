using SampleBase;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid;
using Veldrid.Utilities;

namespace Offscreen
{
    internal class OffscreenApplication : SampleApplication
    {
        private const uint OffscreenWidth = 1024;
        private const uint OffscreenHeight = 1024;

        private DisposeCollectorResourceFactory _factory;
        private CommandList _cl;
        private Framebuffer _offscreenFB;
        private Pipeline _offscreenPipeline;
        private Veldrid.Buffer _uniformBuffer;
        private ResourceSet _phongResourceSet;
        private Model _dragonModel;
        private Model _planeModel;
        private Texture _colorMap;
        private Texture _offscreenColor;
        private TextureView _offscreenView;
        private VertexLayoutDescription _vertexLayout;
        private Pipeline _mainPipeline;
        private Pipeline _mirrorPipeline;

        public OffscreenApplication()
        {
            _factory = new DisposeCollectorResourceFactory(_gd.ResourceFactory);
        }

        protected override void CreateResources(ResourceFactory factory)
        {
            _offscreenColor = factory.CreateTexture(new TextureDescription(
                OffscreenWidth, OffscreenHeight, 1, 1, 1,
                 PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.RenderTarget | TextureUsage.Sampled));
            _offscreenView = factory.CreateTextureView(_offscreenColor);
            Texture offscreenDepth = factory.CreateTexture(new TextureDescription(
                OffscreenWidth, OffscreenHeight, 1, 1, 1, PixelFormat.R16_UNorm, TextureUsage.DepthStencil));
            _offscreenFB = factory.CreateFramebuffer(new FramebufferDescription(offscreenDepth, _offscreenColor));

            _vertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float3),
                new VertexElementDescription("UV", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("Color", VertexElementSemantic.Color, VertexElementFormat.Float3),
                new VertexElementDescription("Position", VertexElementSemantic.Normal, VertexElementFormat.Float3));

            ShaderSetDescription phongShaders = new ShaderSetDescription(
                new[] { _vertexLayout },
                new[]
                {
                    new ShaderStageDescription(ShaderStages.Vertex, LoadShader(factory, "Phong", ShaderStages.Vertex), "VS"),
                    new ShaderStageDescription(ShaderStages.Fragment, LoadShader(factory, "Phong", ShaderStages.Fragment), "FS")
                });

            ResourceLayout phongLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("UBO", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            GraphicsPipelineDescription pd = new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.LessEqual,
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleList,
                phongShaders,
                new[] { phongLayout },
                _offscreenFB.OutputDescription);
            _offscreenPipeline = factory.CreateGraphicsPipeline(pd);

            pd.Outputs = _gd.SwapchainFramebuffer.OutputDescription;
            _mainPipeline = factory.CreateGraphicsPipeline(pd);

            _uniformBuffer = factory.CreateBuffer(new BufferDescription(
                (uint)Unsafe.SizeOf<UniformInfo>(),
                 BufferUsage.UniformBuffer));

            _phongResourceSet = factory.CreateResourceSet(new ResourceSetDescription(phongLayout, _uniformBuffer));

            ResourceLayout mirrorLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("UBO", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                new ResourceLayoutElementDescription("ReflectionMap", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("ReflectionMapSampler", ResourceKind.Sampler, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("ColorMap", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("ColorMapSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            ShaderSetDescription mirrorShaders = new ShaderSetDescription(
                new[] { _vertexLayout },
                new[]
                {
                    new ShaderStageDescription(ShaderStages.Vertex, LoadShader(factory, "Mirror", ShaderStages.Vertex), "VS"),
                    new ShaderStageDescription(ShaderStages.Fragment, LoadShader(factory, "Mirror", ShaderStages.Fragment), "FS")
                });

            GraphicsPipelineDescription mirrorPD = new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.LessEqual,
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleList,
                mirrorShaders,
                new[] { mirrorLayout },
                _gd.SwapchainFramebuffer.OutputDescription);
            _mirrorPipeline = factory.CreateGraphicsPipeline(ref mirrorPD);
        }

        public struct UniformInfo
        {
            public Matrix4x4 Projection;
            public Matrix4x4 Model;
            public Vector4 LightPos;
        }

        protected override void Draw()
        {
            throw new NotImplementedException();
        }

        private void DrawOffscreen()
        {
            _cl.Begin();

            _cl.SetFramebuffer(_offscreenFB);
            _cl.SetFullViewports();
            _cl.ClearColorTarget(0, RgbaFloat.Black);
            _cl.ClearDepthTarget(1f);

            _cl.SetPipeline(_offscreenPipeline);
            _cl.SetGraphicsResourceSet(0, _phongResourceSet);
            _cl.SetVertexBuffer(0, _dragonModel._vertexBuffer);
            _cl.SetIndexBuffer(_dragonModel._indexBuffer, IndexFormat.UInt32);
            _cl.DrawIndexed(_dragonModel._indexCount, 1, 0, 0, 0);

            _cl.End();
        }

        private void LoadAssets()
        {
            void loadAssets()
            {
                _planeModel = new Model();
                VertexLayoutDescription vertexLayout = _vertexLayout;
                _planeModel.LoadFromFile(
                    _gd,
                    _factory,
                    GetAssetPath("models/plane2.dae"),
                    vertexLayout,
                    new Model.ModelCreateInfo(0.5f, 1, 0));

                _dragonModel = new Model();
                _dragonModel.LoadFromFile(
                    _gd,
                    _factory,
                    GetAssetPath("models/chinesedragon.dae"),
                    vertexLayout,
                    new Model.ModelCreateInfo(0.3f, 1, 0));

                _colorMap = LoadTexture(
                    _gd,
                    _factory,
                    GetAssetPath("textures/darkmetal_bc3_unorm.ktx",
                    PixelFormat.BC3_UNorm));
            }
        }

        private unsafe Texture LoadTexture(
            GraphicsDevice gd,
            DisposeCollectorResourceFactory factory,
            string assetPath,
            PixelFormat format)
        {
            KtxFile tex2D;
            using (FileStream fs = File.OpenRead(assetPath))
            {
                tex2D = KtxFile.Load(fs, false);
            }

            uint width = tex2D.Header.PixelWidth;
            uint height = tex2D.Header.PixelHeight;
            if (height == 0) height = width;

            uint mipLevels = tex2D.Header.NumberOfMipmapLevels;

            Texture ret = factory.CreateTexture(new TextureDescription(
                width, height, 1, mipLevels, 1,
                format, TextureUsage.Sampled));

            Texture stagingTex = factory.CreateTexture(new TextureDescription(
                width, height, 1, mipLevels, 1,
                format, TextureUsage.Staging));

            // Copy texture data into staging buffer
            for (uint level = 0; level < mipLevels; level++)
            {
                KtxMipmap mipmap = tex2D.Faces[0].Mipmaps[level];
                byte[] pixelData = mipmap.Data;
                fixed (byte* pixelDataPtr = &pixelData[0])
                {
                    gd.UpdateTexture(stagingTex, (IntPtr)pixelDataPtr, (uint)pixelData.Length,
                        0, 0, 0, mipmap.Width, mipmap.Height, 1, level, 0);
                }
            }

            CommandList copyCL = factory.CreateCommandList();
            copyCL.Begin();
            for (uint level = 0; level < mipLevels; level++)
            {
                uint levelWidth = tex2D.Faces[0].Mipmaps[level].Width;
                uint levelHeight = tex2D.Faces[0].Mipmaps[level].Height;
                copyCL.CopyTexture(stagingTex, 0, 0, 0, level, 0,
                    ret, 0, 0, 0, level, 0, levelWidth, levelHeight, 1, 1);
            }
            copyCL.End();
            gd.ExecuteCommands(copyCL);

            copyCL.Dispose();
            stagingTex.Dispose();

            return ret;
        }

        private static string GetAssetPath(string name)
        {
            return Path.Combine(AppContext.BaseDirectory, "data", name);
        }
    }
}
