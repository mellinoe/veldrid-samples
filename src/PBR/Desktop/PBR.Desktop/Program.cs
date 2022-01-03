using SampleBase;

namespace PBR
{
    class Program
    {
        public static void Main(string[] args)
        {
            VeldridStartupWindow window = new VeldridStartupWindow("PBR");
            PBRApplication offscreen = new PBRApplication(window);
            window.Run();
        }
    }
}
