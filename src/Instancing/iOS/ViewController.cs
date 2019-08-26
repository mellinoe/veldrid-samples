﻿using System;
using Instancing;
using SampleBase;
using SampleBase.iOS;
using UIKit;

namespace iOS
{
    public partial class ViewController : UIViewApplicationWindow
    {
        protected ViewController(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void ViewDidLoad()
        {
			InstancingApplication app = new InstancingApplication(this);
            base.ViewDidLoad();
			// Perform any additional setup after loading the view, typically from a nib.

			Run(new SampleOptions());
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }
    }
}
