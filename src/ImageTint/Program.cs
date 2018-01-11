using SampleBase;
using SixLabors.ImageSharp;
using System;
using System.Numerics;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Veldrid.Utilities;

namespace ImageTint
{
    /*
       ImageTint: This is a sample application which does the following:
         * Loads the image passed in as the first argument, using ImageSharp.
         * Uploads that image to a GPU texture.
         * Renders a new image, with the same dimensions, by sampling the original texture and mixing in a red tint.
         * Copies thew new image into a CPU-visible staging texture
         * Maps the staging texture into CPU address space and copies it into a linear buffer.
         * Constructs a new ImageSharp image from that pixel data array and saves it to a second file.
    */
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine($"ImageTint <image-path> <out>: Tints the image at <image-path> and saves it to <out>.");
                return 1;
            }

            string inPath = args[0];
            string outPath = args[1];

            // This demo uses WindowState.Hidden to avoid popping up an unnecessary window to the user.

            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo
                {
                    WindowInitialState = WindowState.Hidden,
                },
                new GraphicsDeviceOptions(),
                out Sdl2Window window,
                out GraphicsDevice gd);

            DisposeCollectorResourceFactory factory = new DisposeCollectorResourceFactory(gd.ResourceFactory);

            ImageSharpTexture inputImage = new ImageSharpTexture(inPath, false);
            Texture inputTexture = inputImage.CreateDeviceTexture(gd, factory);
            TextureView view = factory.CreateTextureView(inputTexture);

            Texture output = factory.CreateTexture(TextureDescription.Texture2D(
                inputImage.Width,
                inputImage.Height,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.RenderTarget));
            Framebuffer framebuffer = factory.CreateFramebuffer(new FramebufferDescription(null, output));

            DeviceBuffer vertexBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.VertexBuffer));

            Vector4[] quadVerts =
            {
                new Vector4(-1, 1, 0, 0),
                new Vector4(1, 1, 1, 0),
                new Vector4(-1, -1, 0, 1),
                new Vector4(1, -1, 1, 1),
            };
            gd.UpdateBuffer(vertexBuffer, 0, quadVerts);

            ShaderSetDescription shaderSet = new ShaderSetDescription(
                new[]
                {
                    new VertexLayoutDescription(
                        new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float2),
                        new VertexElementDescription("TextureCoordinates", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2))
                },
                new[]
                {
                    SampleApplication.LoadShader(factory, "TintShader", ShaderStages.Vertex, "VS"),
                    SampleApplication.LoadShader(factory, "TintShader", ShaderStages.Fragment, "FS")
                });

            ResourceLayout layout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("Input", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Sampler", ResourceKind.Sampler, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("Tint", ResourceKind.UniformBuffer, ShaderStages.Fragment)));

            Pipeline pipeline = factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.Disabled,
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleStrip,
                shaderSet,
                layout,
                framebuffer.OutputDescription));

            DeviceBuffer tintInfoBuffer = factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));
            gd.UpdateBuffer(
                tintInfoBuffer, 0,
                new TintInfo(
                    new Vector3(1f, 0.2f, 0.1f), // Change this to modify the tint color.
                    0.25f));

            ResourceSet resourceSet = factory.CreateResourceSet(
                new ResourceSetDescription(layout, view, gd.PointSampler, tintInfoBuffer));

            // RenderTarget textures are not CPU-visible, so to get our tinted image back, we need to first copy it into
            // a "staging Texture", which is a Texture that is CPU-visible (it can be Mapped).
            Texture stage = factory.CreateTexture(TextureDescription.Texture2D(
                inputImage.Width,
                inputImage.Height,
                1,
                1,
                PixelFormat.R8_G8_B8_A8_UNorm,
                TextureUsage.Staging));

            CommandList cl = factory.CreateCommandList();
            cl.Begin();
            cl.SetFramebuffer(framebuffer);
            cl.SetFullViewports();
            cl.SetVertexBuffer(0, vertexBuffer);
            cl.SetPipeline(pipeline);
            cl.SetGraphicsResourceSet(0, resourceSet);
            cl.Draw(4, 1, 0, 0);
            cl.CopyTexture(
                output, 0, 0, 0, 0, 0,
                stage, 0, 0, 0, 0, 0,
                stage.Width, stage.Height, 1, 1);
            cl.End();
            gd.SubmitCommands(cl);
            gd.WaitForIdle();

            // When a texture is mapped into a CPU-visible region, it is often not laid out linearly.
            // Instead, it is laid out as a series of rows, which are all spaced out evenly by a "row pitch".
            // This spacing is provided in MappedResource.RowPitch.

            // It is also possible to obtain a "structured view" of a mapped data region, which is what is done below.
            // With a structured view, you can read individual elements from the region.
            // The code below simply iterates over the two-dimensional region and places each texel into a linear buffer.
            // ImageSharp requires the pixel data be contained in a linear buffer.
            MappedResourceView<Rgba32> map = gd.Map<Rgba32>(stage, MapMode.Read);

            // Rgba32 is synonymous with PixelFormat.R8_G8_B8_A8_UNorm.
            Rgba32[] pixelData = new Rgba32[stage.Width * stage.Height];
            for (int y = 0; y < stage.Height; y++)
            {
                for (int x = 0; x < stage.Width; x++)
                {
                    int index = (int)(y * stage.Width + x);
                    pixelData[index] = map[x, y];
                }
            }
            gd.Unmap(stage); // Resources should be Unmapped when the region is no longer used.

            Image<Rgba32> outputImage = Image.LoadPixelData(pixelData, (int)stage.Width, (int)stage.Height);
            outputImage.Save(outPath);

            factory.DisposeCollector.DisposeAll();

            gd.Dispose();
            window.Close();
            return 0;
        }
    }

    public struct TintInfo
    {
        public Vector3 RGBTintColor;
        public float TintFactor;
        public TintInfo(Vector3 rGBTintColor, float tintFactor)
        {
            RGBTintColor = rGBTintColor;
            TintFactor = tintFactor;
        }
    }
}
