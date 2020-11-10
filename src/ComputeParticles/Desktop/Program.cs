using CommandLine;
using SampleBase;

namespace ComputeParticles
{
    class Program
    {
        public static void Main(string[] args)
        {
            VeldridStartupWindow window = new VeldridStartupWindow("Compute Particles");
            ComputeParticles computeParticles = new ComputeParticles(window);
            Parser.Default.ParseArguments<SampleOptions>(args).WithParsed<SampleOptions>(options => window.Run(options));
        }
    }
}
