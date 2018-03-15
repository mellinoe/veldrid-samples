using SampleBase;

namespace AnimatedMesh
{
    internal class Program
    {
        public static void Main()
        {
            VeldridStartupWindow window = new VeldridStartupWindow("AnimatedMesh");
            AnimatedMesh animatedMesh = new AnimatedMesh(window);
            window.Run();
        }
    }
}
