using CommandLine;
using SampleBase;

namespace Offscreen
{
    class Program
    {
        public static void Main(string[] args)
        {
            VeldridStartupWindow window = new VeldridStartupWindow("Offscreen");
            OffscreenApplication offscreen = new OffscreenApplication(window);
            Parser.Default.ParseArguments<SampleOptions>(args).WithParsed<SampleOptions>(options => window.Run(options));
        }
    }
}
