using Android.App;
using Android.Content.PM;
using Android.OS;
using SampleBase.Android;
using System.Threading;
using Veldrid;

namespace TexturedCube.Android
{
    [Activity(
        MainLauncher = true,
        Label = "TexturedCube.Android",
        ConfigurationChanges = ConfigChanges.KeyboardHidden | ConfigChanges.Orientation | ConfigChanges.ScreenSize
        )]
    public class MainActivity : Activity
    {
        private GraphicsDeviceOptions _options;
        private VeldridSurfaceView _view;
        private AndroidApplicationWindow _window;
        private TexturedCube _tc;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            bool debug = false;
#if DEBUG
            debug = true;
#endif

            _options = new GraphicsDeviceOptions(debug, PixelFormat.R16_UNorm, false);
            GraphicsBackend backend = GraphicsDevice.IsBackendSupported(GraphicsBackend.Vulkan)
                ? GraphicsBackend.Vulkan
                : GraphicsBackend.OpenGLES;
            _view = new VeldridSurfaceView(this, backend, _options);
            _window = new AndroidApplicationWindow(this, _view);
            _window.GraphicsDeviceCreated += (g, r, s) => _window.Run();
            _tc = new TexturedCube(_window);
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

