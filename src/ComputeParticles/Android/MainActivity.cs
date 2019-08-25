using Android.App;
using Android.Content.PM;
using Android.OS;
using SampleBase;
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
        private AndroidApplicationWindow _window;

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
            var sampleOptions = new SampleOptions
            {
                Backend = GraphicsDevice.IsBackendSupported(GraphicsBackend.Vulkan)
                    ? GraphicsBackend.Vulkan
                    : GraphicsBackend.OpenGLES
            };
            _view = new VeldridSurfaceView(this, sampleOptions.Backend.Value, options);
            _window = new AndroidApplicationWindow(this, _view);
            _window.GraphicsDeviceCreated += (g, r, s) => _window.Run(sampleOptions);
            ComputeParticles app = new ComputeParticles(_window);
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

