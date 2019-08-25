using CommandLine;
using SampleBase;

namespace AnimatedMesh
{
    class Program
    {
        public static void Main(string[] args)
        {
            VeldridStartupWindow window = new VeldridStartupWindow("Animated Mesh");
            AnimatedMesh animatedMesh = new AnimatedMesh(window);
            Parser.Default.ParseArguments<SampleOptions>(args).WithParsed<SampleOptions>(options => window.Run(options));
        }
    }
}
