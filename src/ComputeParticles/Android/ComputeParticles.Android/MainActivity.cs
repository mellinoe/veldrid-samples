using Android.App;
using Android.Content.PM;
using Android.OS;
using SampleBase.Android;
using Veldrid;

namespace ComputeParticles.Android
{
    [Activity(
        MainLauncher = true,
        Label = "ComputeParticles.Android",
        ConfigurationChanges = ConfigChanges.KeyboardHidden | ConfigChanges.Orientation | ConfigChanges.ScreenSize
        )]
    public class MainActivity : Activity
    {
        private VeldridSurfaceView _view;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            bool debug = false;
#if DEBUG
            debug = true;
#endif

            GraphicsDeviceOptions options = new GraphicsDeviceOptions(debug, PixelFormat.R16_UNorm, false);
            //_view = new VeldridGLESView(this, options);
            _view = new VeldridVulkanView(this, options);
            AndroidApplicationWindow window = new AndroidApplicationWindow(this, _view);
            window.GraphicsDeviceCreated += (g, r, s) => window.Run();
            ComputeParticles app = new ComputeParticles(window);
            SetContentView(_view);
        }

        protected override void OnPause()
        {
            base.OnPause();
            _view.OnPause();
        }

        protected override void OnResume()
        {
            base.OnResume();
            _view.OnResume();
        }
    }
}

