using CommandLine;
using SampleBase;

namespace TexturedCube
{
    class Program
    {
        public static void Main(string[] args)
        {
            VeldridStartupWindow window = new VeldridStartupWindow("Textured Cube");
            TexturedCube texturedCube = new TexturedCube(window);

            Parser.Default.ParseArguments<SampleOptions>(args).WithParsed<SampleOptions>(options => window.Run(options));
        }
    }
}
