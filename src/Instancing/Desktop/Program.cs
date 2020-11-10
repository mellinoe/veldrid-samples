using CommandLine;
using SampleBase;

namespace Instancing
{
    class Program
    {
        public static void Main(string[] args)
        {
            VeldridStartupWindow window = new VeldridStartupWindow("Instancing");
            InstancingApplication instancing = new InstancingApplication(window);
            Parser.Default.ParseArguments<SampleOptions>(args).WithParsed<SampleOptions>(options => window.Run(options));
        }
    }
}
