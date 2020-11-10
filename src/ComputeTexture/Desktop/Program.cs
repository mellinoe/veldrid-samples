using CommandLine;
using SampleBase;

namespace ComputeTexture
{
    class Program
    {
        public static void Main(string[] args)
        {
            VeldridStartupWindow window = new VeldridStartupWindow("Compute Texture");
            ComputeTexture computeTexture = new ComputeTexture(window);
            Parser.Default.ParseArguments<SampleOptions>(args).WithParsed<SampleOptions>(options => window.Run(options));
        }
    }
}
