using System;

using SampleBase.iOS;

namespace TexturedCube.iOS
{
	public partial class ViewController : UIViewApplicationWindow
    {
        protected ViewController(IntPtr handle) : base(handle)
        {
			// Note: this .ctor should not contain any initialization logic.
        }

        public override void ViewDidLoad()
        {
			TexturedCube tc = new TexturedCube(this);
            base.ViewDidLoad();
			// Perform any additional setup after loading the view, typically from a nib.

			Run();
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }
    }
}
