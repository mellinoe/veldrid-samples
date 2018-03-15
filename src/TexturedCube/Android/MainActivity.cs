using Android.App;
using Android.OS;
using SampleBase.Android;
using Veldrid;

namespace TexturedCube.Android
{
    [Activity(Label = "TexturedCube.Android", MainLauncher = true)]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            bool debug = false;
#if DEBUG
            debug = true;
#endif
            GraphicsDeviceOptions options = new GraphicsDeviceOptions(debug, PixelFormat.R16_UNorm, true);
            AndroidApplicationWindow window = new AndroidApplicationWindow(this, options);
            SetContentView(window);
            TexturedCube tc = new TexturedCube(window);
            window.GraphicsDeviceCreated += (g, r) => window.Run();
        }
    }
}

