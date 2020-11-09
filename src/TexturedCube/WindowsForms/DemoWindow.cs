using System.Windows.Forms;

namespace TexturedCube
{
    public partial class DemoWindow : Form
    {
        private TexturedCube _texturedCube;

        public DemoWindow()
        {
            InitializeComponent();
            var veldridControl = new VeldridControl {IsAnimated = true};
            veldridControl.Dock = DockStyle.Fill;
            _texturedCube = new TexturedCube(veldridControl);
            Controls.Add(veldridControl);
        }
    }
}