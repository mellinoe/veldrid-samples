using System;
using System.Windows;
using System.Windows.Media;
using Application = System.Windows.Forms.Application;

namespace TexturedCube
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TexturedCube _texturedCube;
        private readonly VeldridControl _veldridControl;

        public MainWindow()
        {
            InitializeComponent();

            // Subscribe to render events to trigger WinForms Application.Idle event
            CompositionTarget.Rendering += OnRendering;

            _veldridControl = new VeldridControl {IsAnimated = true};
            _texturedCube = new TexturedCube(_veldridControl);
            _host.Child = _veldridControl;
        }

        protected override void OnClosed(EventArgs e)
        {
            // Unsubscribe from render events
            CompositionTarget.Rendering -= OnRendering;
            base.OnClosed(e);
        }

        private void OnRendering(object sender, EventArgs e)
        {
            Application.RaiseIdle(e);
        }
    }
}