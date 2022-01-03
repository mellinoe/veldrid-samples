using Android.Content.PM;
using SampleBase.Android;
using Veldrid;

namespace PBR.Android
{
    [Activity(
        MainLauncher = true,
        Label = "PBR.Android",
        ConfigurationChanges = ConfigChanges.KeyboardHidden | ConfigChanges.Orientation | ConfigChanges.ScreenSize,
        Theme = "@android:style/Theme.DeviceDefault.NoActionBar.Fullscreen"
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

            GraphicsDeviceOptions options = new GraphicsDeviceOptions(
                debug,
                PixelFormat.R16_UNorm,
                false,
                ResourceBindingModel.Improved,
                true,
                true);
            GraphicsBackend backend = GraphicsDevice.IsBackendSupported(GraphicsBackend.Vulkan)
                ? GraphicsBackend.Vulkan
                : GraphicsBackend.OpenGLES;
            _view = new VeldridSurfaceView(this, backend, options);
            var window = new AndroidApplicationWindow(this, _view);
            window.GraphicsDeviceCreated += (g, r, s) => window.Run();
            var app = new PBRApplication(window);
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