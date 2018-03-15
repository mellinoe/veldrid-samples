using System;
using System.Runtime.InteropServices;

namespace Veldrid.Android
{
    public static class AndroidRuntime
    {
        public const string LibName = "android.so";

        [DllImport(LibName)]
        public static extern IntPtr ANativeWindow_fromSurface(IntPtr env, IntPtr surface);
        [DllImport(LibName)]
        public static extern int ANativeWindow_setBuffersGeometry(IntPtr aNativeWindow, int width, int height, int format);
        [DllImport(LibName)]
        public static extern void ANativeWindow_release(IntPtr aNativeWindow);
    }
}
