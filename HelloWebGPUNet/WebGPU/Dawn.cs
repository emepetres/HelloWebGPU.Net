using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using WaveEngine.Bindings.WebGPU;

namespace HelloWebGPUNet.WebGPU
{
    using HWND = IntPtr;
    using WGPUDevice = IntPtr; // FIXME remove when implemented in binding

    public class Dawn
    {
        [DllImport("Dawn.Native.dll")]
        public static extern unsafe WGPUDevice createDevice(HWND handle, WGPUBackendType type = WGPUBackendType.WGPUBackendType_Force32);
    }
}
