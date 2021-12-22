using System;
using System.Numerics;
using System.Runtime.InteropServices;
using AssetPrimitives;
using SampleBase;
using Veldrid;
using Veldrid.SPIRV;

namespace PBR
{
    public class PBRApplication : SampleApplication
    {
        private const bool Debug
#if DEBUG
            = true;
#else
            = false;
#endif

        private Pipeline _pbrPipeline;
        private Pipeline _skyboxPipeline;

        private ProcessedModel _model;
        private ProcessedMeshPart _modelMesh;
        private ProcessedModel _skybox;
        private ProcessedMeshPart _skyboxMesh;

        private Texture _modelAlbedoTexture;
        private Texture _modelNormalTexture;
        private Texture _modelMetalnessTexture;
        private Texture _modelRoughnessTexture;
        private Texture _environmentSpecularTexture;
        private Texture _modelIrradianceTexture;
        private Texture _modelSpecularBRDF_LUTTexture;

        private DeviceBuffer _modelVertexBuffer;
        private DeviceBuffer _modelIndexBuffer;
        private ResourceSet _modelUniformsResourceSet;
        private ResourceSet _skyboxUniformsResourceSet;
        private ResourceSet _modelSamplerUniformsResourceSet;
        private ResourceSet _skyboxSamplerResourceSet;

        private DeviceBuffer _skyboxVertexBuffer;
        private DeviceBuffer _skyboxIndexBuffer;

        private DeviceBuffer _matrixBuffer;
        private DeviceBuffer _shadingBuffer;

        private CommandList _commandList;

        [StructLayout(LayoutKind.Sequential)]
        private struct FakePushConstants
        {
            public uint level;
            public float roughness;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct AnalyticalLight
        {
            public Vector4 Direction { get; set; }
            public Vector4 Radiance { get; set; }
        }

        public PBRApplication(ApplicationWindow window) : base(window)
        {
            SpirvCompilation.SetDefaultTargetVersionForCrossCompileTarget(CrossCompileTarget.MSL, SpirvCompilation.MakeMSLVersion(2, 1));
        }

        private const uint NumLights = 3;
        private const uint EnvironmentCubeMapSize = 1024;
        private static readonly uint EnvironmentCubeMapMipLevels = ComputeMipLevels(EnvironmentCubeMapSize, EnvironmentCubeMapSize);
        private const uint kIrradianceMapSize = 32;
        private const uint kBRDF_LUT_Size = 256;

        private static uint ComputeMipLevels(uint width, uint height)
            => 1 + (uint)Math.Floor(Math.Log(Math.Max(width, height), 2));

        private readonly static uint SizeOfMatrix = (uint)Marshal.SizeOf<Matrix4x4>();
        private readonly static uint SizeOfLight = (uint)Marshal.SizeOf<AnalyticalLight>();
        private readonly static uint SizeOfVector4 = (uint)Marshal.SizeOf<Vector4>();

        private void SetViewProjection(Matrix4x4 matrix)
            => GraphicsDevice.UpdateBuffer(_matrixBuffer, SizeOfMatrix * 0, ref matrix);

        public void SetSkyProjection(Matrix4x4 matrix)
            => GraphicsDevice.UpdateBuffer(_matrixBuffer, SizeOfMatrix * 1, ref matrix);

        public void SetSceneRotation(Matrix4x4 matrix)
            => GraphicsDevice.UpdateBuffer(_matrixBuffer, SizeOfMatrix * 2, ref matrix);

        public void SetLight(uint lightIndex, ref AnalyticalLight light)
        {
            if (lightIndex >= NumLights)
                throw new ArgumentOutOfRangeException($"{nameof(SetLight)} {nameof(lightIndex)} must be less than {NumLights}");
            GraphicsDevice.UpdateBuffer(_shadingBuffer, SizeOfLight * lightIndex, ref light);
        }

        public void SetLight(uint lightIndex, AnalyticalLight light)
            => SetLight(lightIndex, ref light);

        public void SetEyePosition(Vector3 eyePosition)
            => GraphicsDevice.UpdateBuffer(_shadingBuffer, SizeOfLight * NumLights, ref eyePosition);

        private (Texture SpecularEnvironmentCubemap, Texture IrradianceEnvironmentCubemap, Texture SpecularBRDF_LUT) LoadAndPreprocessEnvironmentMap(ResourceFactory factory, Sampler textureSampler)
        {
            var environmentTex = LoadEmbeddedAsset<ProcessedTexture>("environment.binary");

            using (var unfilteredEnvCubeMap = factory.CreateTexture(TextureDescription.Texture2D(EnvironmentCubeMapSize, EnvironmentCubeMapSize, EnvironmentCubeMapMipLevels, 1, PixelFormat.R16_G16_B16_A16_Float, TextureUsage.Sampled | TextureUsage.Cubemap | TextureUsage.GenerateMipmaps)))
            {
                using (var environmentHdrTexture = environmentTex.CreateDeviceTexture(GraphicsDevice, factory, TextureUsage.Sampled))
                {
                    { // convert hdr to cubemap
                        using (var temporaryUnfilteredEnvCubeMap = factory.CreateTexture(TextureDescription.Texture2D(EnvironmentCubeMapSize, EnvironmentCubeMapSize, 1, 6, PixelFormat.R16_G16_B16_A16_Float, TextureUsage.Storage | TextureUsage.Sampled | TextureUsage.RenderTarget)))
                        {
                            var equirect2CubeShaderSrc = ReadEmbeddedAssetBytes("equirect2cube_cs.glsl");
                            var equirect2CubeUniformLayoutDescription = new ResourceLayoutDescription(
                                new ResourceLayoutElementDescription("outputTexture", ResourceKind.TextureReadWrite, ShaderStages.Compute),
                                new ResourceLayoutElementDescription("inputTexture", ResourceKind.TextureReadOnly, ShaderStages.Compute),
                                new ResourceLayoutElementDescription("textureSampler", ResourceKind.Sampler, ShaderStages.Compute)
                            );
                            
                            using (var shader = factory.CreateFromSpirv(new ShaderDescription(ShaderStages.Compute, equirect2CubeShaderSrc, "main", Debug)))
                            using (var equirect2CubeUniformLayout = factory.CreateResourceLayout(ref equirect2CubeUniformLayoutDescription))
                            using (var equirect2CubeResourceSet = factory.CreateResourceSet(new ResourceSetDescription(equirect2CubeUniformLayout, temporaryUnfilteredEnvCubeMap, environmentHdrTexture, textureSampler)))
                            using (var pipeline = factory.CreateComputePipeline(new ComputePipelineDescription(shader, equirect2CubeUniformLayout, 32, 32, 1)))
                            using (var commandList = factory.CreateCommandList())
                            {
                                commandList.Begin();
                                commandList.SetPipeline(pipeline);
                                commandList.SetComputeResourceSet(0, equirect2CubeResourceSet);
                                commandList.Dispatch(EnvironmentCubeMapSize / 32, EnvironmentCubeMapSize / 32, 6);
                                for (uint layer = 0; layer < temporaryUnfilteredEnvCubeMap.ArrayLayers; layer++)
                                    commandList.CopyTexture(temporaryUnfilteredEnvCubeMap, unfilteredEnvCubeMap, 0, layer);
                                commandList.End();
                                GraphicsDevice.SubmitCommands(commandList);
                                GraphicsDevice.WaitForIdle();

                                commandList.Begin();
                                commandList.GenerateMipmaps(unfilteredEnvCubeMap);
                                commandList.End();
                                GraphicsDevice.SubmitCommands(commandList);
                                GraphicsDevice.WaitForIdle();
                            }
                        }
                    }

                    var specularEnvironmentMapTexture = factory.CreateTexture(TextureDescription.Texture2D(EnvironmentCubeMapSize, EnvironmentCubeMapSize, EnvironmentCubeMapMipLevels, 1, PixelFormat.R16_G16_B16_A16_Float, TextureUsage.Sampled | TextureUsage.Cubemap));
                    { // compute pre-filtered specular environment map
                        using (var temporarySpecularEnvironmentMapTexture = factory.CreateTexture(TextureDescription.Texture2D(EnvironmentCubeMapSize, EnvironmentCubeMapSize, EnvironmentCubeMapMipLevels, 6, PixelFormat.R16_G16_B16_A16_Float, TextureUsage.Storage | TextureUsage.Sampled | TextureUsage.RenderTarget)))
                        {
                            var spmapShaderSrc = ReadEmbeddedAssetBytes("spmap_cs.glsl");
                            var spmapUniformLayoutDescription = new ResourceLayoutDescription(
                                new ResourceLayoutElementDescription("outputTexture", ResourceKind.TextureReadWrite, ShaderStages.Compute),
                                new ResourceLayoutElementDescription("inputTexture", ResourceKind.TextureReadOnly, ShaderStages.Compute),
                                new ResourceLayoutElementDescription("FakePushConstants", ResourceKind.UniformBuffer, ShaderStages.Compute),
                                new ResourceLayoutElementDescription("textureSampler", ResourceKind.Sampler, ShaderStages.Compute)
                            );

                            using (var shader = factory.CreateFromSpirv(new ShaderDescription(ShaderStages.Compute, spmapShaderSrc, "main", Debug)))
                            using (var spmapUniformLayout = factory.CreateResourceLayout(ref spmapUniformLayoutDescription))
                            using (var roughnessFilterBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer)))
                            using (var pipeline = factory.CreateComputePipeline(new ComputePipelineDescription(shader, spmapUniformLayout, 32, 32, 1, new SpecializationConstant[] { new SpecializationConstant(0, 1) })))
                            using (var commandList = factory.CreateCommandList())
                            {
                                commandList.Begin();
                                commandList.CopyTexture(unfilteredEnvCubeMap, temporarySpecularEnvironmentMapTexture);
                                commandList.End();
                                GraphicsDevice.SubmitCommands(commandList);
                                GraphicsDevice.WaitForIdle();

                                var deltaRoughness = 1.0f / Math.Max(EnvironmentCubeMapMipLevels - 1, 1.0f);
                                for (uint level = 1, size = EnvironmentCubeMapSize / 2; level < EnvironmentCubeMapMipLevels; ++level, size /= 2)
                                {
                                    using (var mipTailTexView = factory.CreateTextureView(new TextureViewDescription(temporarySpecularEnvironmentMapTexture, PixelFormat.R16_G16_B16_A16_Float, level, 1, 0, 6)))
                                    using (var resourceSet = factory.CreateResourceSet(new ResourceSetDescription(spmapUniformLayout, mipTailTexView, unfilteredEnvCubeMap, roughnessFilterBuffer, textureSampler)))
                                    {
                                        var numGroups = Math.Max(1, size / 32);
                                        GraphicsDevice.UpdateBuffer(roughnessFilterBuffer, 0, new FakePushConstants { level = 0, roughness = level * deltaRoughness });

                                        commandList.Begin();
                                        commandList.SetPipeline(pipeline);
                                        commandList.SetComputeResourceSet(0, resourceSet);
                                        commandList.Dispatch(numGroups, numGroups, 6);

                                        commandList.End();

                                        GraphicsDevice.SubmitCommands(commandList);
                                        GraphicsDevice.WaitForIdle();
                                    }
                                }

                                commandList.Begin();
                                commandList.CopyTexture(temporarySpecularEnvironmentMapTexture, specularEnvironmentMapTexture);
                                commandList.End();
                                GraphicsDevice.SubmitCommands(commandList);
                                GraphicsDevice.WaitForIdle();
                            }
                        }
                    }

                    var irradianceEnvironmentMapTexture = factory.CreateTexture(TextureDescription.Texture2D(kIrradianceMapSize, kIrradianceMapSize, 1, 1, PixelFormat.R16_G16_B16_A16_Float, TextureUsage.Sampled | TextureUsage.Cubemap));
                    {
                        using (var temporaryIrradianceEnvironmentMapTexture = factory.CreateTexture(TextureDescription.Texture2D(kIrradianceMapSize, kIrradianceMapSize, 1, 6, PixelFormat.R16_G16_B16_A16_Float, TextureUsage.Storage | TextureUsage.Sampled | TextureUsage.RenderTarget)))
                        {
                            var irradianceShaderSrc = ReadEmbeddedAssetBytes("irmap_cs.glsl");
                            var irradianceUniformLayoutDescription = new ResourceLayoutDescription(
                                new ResourceLayoutElementDescription("outputTexture", ResourceKind.TextureReadWrite, ShaderStages.Compute),
                                new ResourceLayoutElementDescription("inputTexture", ResourceKind.TextureReadOnly, ShaderStages.Compute),
                                new ResourceLayoutElementDescription("textureSampler", ResourceKind.Sampler, ShaderStages.Compute)
                            );

                            using (var shader = factory.CreateFromSpirv(new ShaderDescription(ShaderStages.Compute, irradianceShaderSrc, "main", Debug)))
                            using (var irradianceUniformLayout = factory.CreateResourceLayout(irradianceUniformLayoutDescription))
                            using (var irradianceResourceSet = factory.CreateResourceSet(new ResourceSetDescription(irradianceUniformLayout, temporaryIrradianceEnvironmentMapTexture, unfilteredEnvCubeMap, textureSampler)))
                            using (var pipeline = factory.CreateComputePipeline(new ComputePipelineDescription(shader, irradianceUniformLayout, 32, 32, 1)))
                            using (var commandList = factory.CreateCommandList())
                            {
                                commandList.Begin();

                                commandList.SetPipeline(pipeline);
                                commandList.SetComputeResourceSet(0, irradianceResourceSet);
                                commandList.Dispatch(kIrradianceMapSize / 32, kIrradianceMapSize / 32, 6);

                                commandList.CopyTexture(temporaryIrradianceEnvironmentMapTexture, irradianceEnvironmentMapTexture);

                                commandList.End();
                                GraphicsDevice.SubmitCommands(commandList);
                                GraphicsDevice.WaitForIdle();
                            }
                        }
                    }

                    var specularBRDFLUTTexture = factory.CreateTexture(TextureDescription.Texture2D(kBRDF_LUT_Size, kBRDF_LUT_Size, 1, 1, PixelFormat.R16_G16_Float, TextureUsage.Storage | TextureUsage.Sampled));
                    { // compute SpecularBRDF_LUT
                        var spbrdfShaderSrc = ReadEmbeddedAssetBytes("spbrdf_cs.glsl");

                        var spbrdfUniformLayoutDescription = new ResourceLayoutDescription(
                            new ResourceLayoutElementDescription("LUT", ResourceKind.TextureReadWrite, ShaderStages.Compute)
                        );

                        using (var shader = factory.CreateFromSpirv(new ShaderDescription(ShaderStages.Compute, spbrdfShaderSrc, "main", Debug)))
                        using (var spbrdfUniformLayout = factory.CreateResourceLayout(spbrdfUniformLayoutDescription))
                        using (var spbrdfResourceSet = factory.CreateResourceSet(new ResourceSetDescription(spbrdfUniformLayout, specularBRDFLUTTexture)))
                        using (var pipeline = factory.CreateComputePipeline(new ComputePipelineDescription(shader, spbrdfUniformLayout, 32, 32, 1)))
                        using (var commandList = factory.CreateCommandList())
                        {
                            commandList.Begin();

                            commandList.SetPipeline(pipeline);
                            commandList.SetComputeResourceSet(0, spbrdfResourceSet);
                            commandList.Dispatch(kBRDF_LUT_Size / 32, kBRDF_LUT_Size / 32, 6);

                            commandList.End();
                            GraphicsDevice.SubmitCommands(commandList);
                            GraphicsDevice.WaitForIdle();
                        }
                    }

                    if (GraphicsDevice.GetVulkanInfo(out var info))
                    {
                        info.TransitionImageLayout(specularEnvironmentMapTexture, (uint)Vulkan.VkImageLayout.ShaderReadOnlyOptimal);
                        info.TransitionImageLayout(irradianceEnvironmentMapTexture, (uint)Vulkan.VkImageLayout.ShaderReadOnlyOptimal);
                        info.TransitionImageLayout(specularBRDFLUTTexture, (uint)Vulkan.VkImageLayout.ShaderReadOnlyOptimal);
                    }

                    return (specularEnvironmentMapTexture, irradianceEnvironmentMapTexture, specularBRDFLUTTexture);
                }
            }
        }

        protected override void CreateResources(ResourceFactory factory)
        {
            _commandList = factory.CreateCommandList();

            _model = LoadEmbeddedAsset<ProcessedModel>("cerberus.binary");
            _modelMesh = _model.MeshParts[0];
            _skybox = LoadEmbeddedAsset<ProcessedModel>("skybox.binary");
            _skyboxMesh = _skybox.MeshParts[0];

            {
                var albedoTex = LoadEmbeddedAsset<ProcessedTexture>("cerberus_A.binary");
                _modelAlbedoTexture = albedoTex.CreateDeviceTexture(GraphicsDevice, factory, TextureUsage.Sampled);
            }
            {
                var normalTex = LoadEmbeddedAsset<ProcessedTexture>("cerberus_N.binary");
                _modelNormalTexture = normalTex.CreateDeviceTexture(GraphicsDevice, factory, TextureUsage.Sampled);
            }
            {
                var metalnessTex = LoadEmbeddedAsset<ProcessedTexture>("cerberus_M.binary");
                _modelMetalnessTexture = metalnessTex.CreateDeviceTexture(GraphicsDevice, factory, TextureUsage.Sampled);
            }
            {
                var roughnessTex = LoadEmbeddedAsset<ProcessedTexture>("cerberus_R.binary");
                _modelRoughnessTexture = roughnessTex.CreateDeviceTexture(GraphicsDevice, factory, TextureUsage.Sampled);
            }

            var textureSampler = GraphicsDevice.Aniso4xSampler;
            (_environmentSpecularTexture, _modelIrradianceTexture, _modelSpecularBRDF_LUTTexture) = LoadAndPreprocessEnvironmentMap(factory, textureSampler);

            var modelVertexLayout = new VertexLayoutDescription(_modelMesh.VertexElements);
            _modelVertexBuffer = factory.CreateBuffer(new BufferDescription((uint)_modelMesh.VertexData.Length, BufferUsage.VertexBuffer));
            GraphicsDevice.UpdateBuffer(_modelVertexBuffer, 0, _modelMesh.VertexData);

            _modelIndexBuffer = factory.CreateBuffer(new BufferDescription((uint)_modelMesh.IndexData.Length, BufferUsage.IndexBuffer));
            GraphicsDevice.UpdateBuffer(_modelIndexBuffer, 0, _modelMesh.IndexData);

            _matrixBuffer = factory.CreateBuffer(new BufferDescription(SizeOfMatrix * 3, BufferUsage.UniformBuffer));
            _shadingBuffer = factory.CreateBuffer(new BufferDescription((SizeOfLight * NumLights) + SizeOfVector4, BufferUsage.UniformBuffer));

            var transformAndShadingUniformsLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("TransformUniforms", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("ShadingUniforms", ResourceKind.UniformBuffer, ShaderStages.Fragment)
                )
            );
            var transformOnlyUniformsLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("TransformUniforms", ResourceKind.UniformBuffer, ShaderStages.Vertex)
                )
            );

            var fsSamplerUniformsLayoutDescription = new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("albedoTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("normalTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("metalnessTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("roughnessTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("specularTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("irradianceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("specularBRDF_LUT", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("textureSampler", ResourceKind.Sampler, ShaderStages.Fragment)
            );
            var fsSamplerUniformsLayout = factory.CreateResourceLayout(ref fsSamplerUniformsLayoutDescription);

            Shader[] pbrShaders = factory.CreateFromSpirv(
                new ShaderDescription(ShaderStages.Vertex, ReadEmbeddedAssetBytes("pbr_vs.glsl"), "main", Debug),
                new ShaderDescription(ShaderStages.Fragment, ReadEmbeddedAssetBytes("pbr_fs.glsl"), "main", Debug)
            );

            _pbrPipeline = factory.CreateGraphicsPipeline(
                new GraphicsPipelineDescription()
                {
                    BlendState = BlendStateDescription.SingleOverrideBlend,
                    DepthStencilState = new DepthStencilStateDescription(
                            depthTestEnabled: true,
                            depthWriteEnabled: true,
                            comparisonKind: ComparisonKind.LessEqual
                        ),
                    RasterizerState = new RasterizerStateDescription(
                            cullMode: FaceCullMode.Back,
                            fillMode: PolygonFillMode.Solid,
                            frontFace: FrontFace.Clockwise,
                            depthClipEnabled: true,
                            scissorTestEnabled: false
                        ),
                    PrimitiveTopology = PrimitiveTopology.TriangleList,
                    ResourceLayouts = new[] { transformAndShadingUniformsLayout, fsSamplerUniformsLayout },
                    ShaderSet = new ShaderSetDescription(
                            vertexLayouts: new[] { modelVertexLayout },
                            shaders: pbrShaders
                        ),
                    Outputs = GraphicsDevice.SwapchainFramebuffer.OutputDescription
                }
            );
            var skyboxVertexLayout = new VertexLayoutDescription(_skyboxMesh.VertexElements);

            var skyboxShaders = factory.CreateFromSpirv(
                new ShaderDescription(ShaderStages.Vertex, ReadEmbeddedAssetBytes("skybox_vs.glsl"), "main", Debug),
                new ShaderDescription(ShaderStages.Fragment, ReadEmbeddedAssetBytes("skybox_fs.glsl"), "main", Debug)
            );
            var skyboxSamplerUniformsLayoutDescription = new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("envTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("textureSampler", ResourceKind.Sampler, ShaderStages.Fragment)
            );
            var skyboxSamplerUniformsLayout = factory.CreateResourceLayout(ref skyboxSamplerUniformsLayoutDescription);

            _skyboxPipeline = factory.CreateGraphicsPipeline(
                new GraphicsPipelineDescription()
                {
                    BlendState = BlendStateDescription.SingleOverrideBlend,
                    DepthStencilState = new DepthStencilStateDescription(
                            depthTestEnabled: false,
                            depthWriteEnabled: false,
                            comparisonKind: ComparisonKind.LessEqual
                        ),
                    RasterizerState = new RasterizerStateDescription(
                            cullMode: FaceCullMode.Back,
                            fillMode: PolygonFillMode.Solid,
                            frontFace: FrontFace.Clockwise,
                            depthClipEnabled: false,
                            scissorTestEnabled: true
                        ),
                    PrimitiveTopology = PrimitiveTopology.TriangleList,
                    ResourceLayouts = new[] { transformOnlyUniformsLayout, skyboxSamplerUniformsLayout },
                    ShaderSet = new ShaderSetDescription(
                            vertexLayouts: new[] { skyboxVertexLayout },
                            shaders: skyboxShaders
                        ),
                    Outputs = GraphicsDevice.SwapchainFramebuffer.OutputDescription
                }
            );

            _skyboxVertexBuffer = factory.CreateBuffer(new BufferDescription((uint)_skyboxMesh.VertexData.Length, BufferUsage.VertexBuffer));
            GraphicsDevice.UpdateBuffer(_skyboxVertexBuffer, 0, _skyboxMesh.VertexData);
            _skyboxIndexBuffer = factory.CreateBuffer(new BufferDescription((uint)_skyboxMesh.VertexData.Length, BufferUsage.IndexBuffer));
            GraphicsDevice.UpdateBuffer(_skyboxIndexBuffer, 0, _skyboxMesh.IndexData);

            _modelUniformsResourceSet = factory.CreateResourceSet(new ResourceSetDescription(transformAndShadingUniformsLayout, _matrixBuffer, _shadingBuffer));
            _skyboxUniformsResourceSet = factory.CreateResourceSet(new ResourceSetDescription(transformOnlyUniformsLayout, _matrixBuffer));
            _modelSamplerUniformsResourceSet = factory.CreateResourceSet(new ResourceSetDescription(fsSamplerUniformsLayout, _modelAlbedoTexture, _modelNormalTexture, _modelMetalnessTexture, _modelRoughnessTexture, _environmentSpecularTexture, _modelIrradianceTexture, _modelSpecularBRDF_LUTTexture, textureSampler));
            _skyboxSamplerResourceSet = factory.CreateResourceSet(new ResourceSetDescription(skyboxSamplerUniformsLayout, _environmentSpecularTexture, textureSampler));

            SetSceneRotation(Matrix4x4.Identity);

            var WhiteLight = new Vector3(1f, 1f, 1f);
            var LightOff = Vector3.Zero;

            var lights = new AnalyticalLight[] {
                new AnalyticalLight(){ Direction = new Vector4(-1.0f, 0.0f, 0.0f, 0.0f), Radiance = new Vector4(LightOff, 0f) },
                new AnalyticalLight(){ Direction = new Vector4(1.0f, 0.0f, 0.0f, 0.0f), Radiance = new Vector4(LightOff, 0f) },
                new AnalyticalLight(){ Direction = new Vector4(0.0f, -1.0f, 0.0f, 0.0f), Radiance = new Vector4(LightOff, 0f) },
            };
            SetLight(0, ref lights[0]);
            SetLight(1, ref lights[1]);
            SetLight(2, ref lights[2]);

            _camera.Position = new Vector3(0, 0, 150f);
            _camera.MoveSpeed = 500f;
            _camera.NearDistance = 4f;
            _camera.FarDistance = 4096f;
            _camera.Yaw = 0f;
            _camera.Pitch = 0f;
        }

        protected override void Draw(float deltaSeconds)
        {
            Matrix4x4.Decompose(_camera.ViewMatrix, out var viewScale, out var viewRotation, out var viewTranslation);
            var viewRotationMatrix = Matrix4x4.CreateScale(viewScale) * Matrix4x4.CreateFromQuaternion(viewRotation);

            SetEyePosition(_camera.Position);
            SetViewProjection(_camera.ViewMatrix * _camera.ProjectionMatrix);
            SetSkyProjection(viewRotationMatrix * _camera.ProjectionMatrix);

            {
                _commandList.Begin();
                _commandList.SetFramebuffer(GraphicsDevice.SwapchainFramebuffer);
                _commandList.ClearColorTarget(0, RgbaFloat.CornflowerBlue);
                _commandList.ClearDepthStencil(1f);

                _commandList.SetPipeline(_skyboxPipeline);
                _commandList.SetVertexBuffer(0, _skyboxVertexBuffer);
                _commandList.SetIndexBuffer(_skyboxIndexBuffer, _skyboxMesh.IndexFormat);
                _commandList.SetGraphicsResourceSet(0, _skyboxUniformsResourceSet);
                _commandList.SetGraphicsResourceSet(1, _skyboxSamplerResourceSet);
                _commandList.DrawIndexed(_skyboxMesh.IndexCount);

                _commandList.SetPipeline(_pbrPipeline);
                _commandList.SetVertexBuffer(0, _modelVertexBuffer);
                _commandList.SetIndexBuffer(_modelIndexBuffer, _modelMesh.IndexFormat);
                _commandList.SetGraphicsResourceSet(0, _modelUniformsResourceSet);
                _commandList.SetGraphicsResourceSet(1, _modelSamplerUniformsResourceSet);
                _commandList.DrawIndexed(_modelMesh.IndexCount);

                _commandList.End();
                GraphicsDevice.SubmitCommands(_commandList);
            }

            GraphicsDevice.SwapBuffers();
        }
    }
}