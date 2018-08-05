using SampleBase;

namespace Swapchains
{
    class Program
    {
        static void Main(string[] args)
        {
            VeldridStartupWindow window = new VeldridStartupWindow("Swapchains Demo");
            SwapchainApplication app = new SwapchainApplication(window);
            window.Run();
        }
    }
}
