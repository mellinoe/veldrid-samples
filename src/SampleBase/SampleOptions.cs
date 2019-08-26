using CommandLine;
using Veldrid;

namespace SampleBase
{
    public class SampleOptions
    {
        [Option('g',"graphics", HelpText = "Graphics device. Should be equal to one of the following values: Direct3D11, Vulkan, OpenGL, Metal or OpenGLES.")]
        public GraphicsBackend? Backend { get; set; }

        [Option('r',"renderdoc", HelpText = "Enable RenderDoc")]
        public bool Renderdoc { get; set; }
    }
}