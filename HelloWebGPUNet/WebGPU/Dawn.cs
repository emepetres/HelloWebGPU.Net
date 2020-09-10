using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using WaveEngine.Bindings.WebGPU;

namespace HelloWebGPUNet.WebGPU
{
    using HWND = IntPtr;
    using WGPUDevice = IntPtr;
    using WGPUSwapChain = IntPtr;
    using WGPUProc = IntPtr;

    public class Dawn
    {
        [DllImport("Dawn.Native.dll")]
        public static extern WGPUDevice createDevice(HWND handle, WGPUBackendType type = WGPUBackendType.WGPUBackendType_Force32);

        [DllImport("Dawn.Native.dll")]
        public static extern WGPUSwapChain createSwapChain(WGPUDevice device);

        [DllImport("Dawn.Native.dll")]
        public static extern WGPUTextureFormat getSwapChainFormat(WGPUDevice device);

        [DllImport("Dawn.Native.dll")]
        public static extern WGPUProc getProcAddress(WGPUDevice device, string procName);
    }
}
