using System;
using System.Runtime.InteropServices;
using WaveEngine.Bindings.WebGPU;

namespace HelloWebGPUNet.Web.WebGPU
{
    using WGPUDevice = IntPtr;
    using WGPUSwapChain = IntPtr;
    using WGPUSurface = IntPtr;

    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class MonoPInvokeCallbackAttribute : Attribute
    {
        public MonoPInvokeCallbackAttribute(Type type)
        {
            Type = type;
        }

        public Type Type { get; private set; }
    }

    public unsafe class Emscripten
    {
        private static char* canvas_str = (char*)Marshal.StringToHGlobalAnsi("canvas");

        public delegate bool Loop();
        public delegate int EMLoop(double time, void* userData);

        [DllImport("libWebGPU")]
        private static extern WGPUDevice test_emscripten_webgpu_get_device();

        [DllImport("libWebGPU")]
        private static extern WGPUDevice emscripten_request_animation_frame_loop(EMLoop em_loop, void* userData);

        public static WGPUDevice CreateDevice(IntPtr _)
        {
            return test_emscripten_webgpu_get_device();
        }

        public static WGPUSwapChain CreateSwapChain(WGPUDevice device)
        {
            WGPUSurfaceDescriptorFromCanvasHTMLSelector canvDesc = new WGPUSurfaceDescriptorFromCanvasHTMLSelector
            {
                chain = new WGPUChainedStruct
                {
                    sType = WGPUSType.WGPUSType_SurfaceDescriptorFromCanvasHTMLSelector,
                },
                selector = canvas_str
            };

			WGPUSurfaceDescriptor surfDesc = new WGPUSurfaceDescriptor
            {
                nextInChain = (WGPUChainedStruct*)&canvDesc
            };

			WGPUSurface surface = WebGPUNative.wgpuInstanceCreateSurface(IntPtr.Zero, &surfDesc);

            WGPUSwapChainDescriptor swapDesc = new WGPUSwapChainDescriptor
            {
                usage = WGPUTextureUsage.WGPUTextureUsage_OutputAttachment,
                format = WGPUTextureFormat.WGPUTextureFormat_BGRA8Unorm,
                width = 800,
                height = 450,
                presentMode = WGPUPresentMode.WGPUPresentMode_Fifo
            };

			return WebGPUNative.wgpuDeviceCreateSwapChain(device, surface, &swapDesc); ;
		}

        public static WGPUTextureFormat GetSwapChainFormat(WGPUDevice _)
        {
            return WGPUTextureFormat.WGPUTextureFormat_BGRA8Unorm;
        }

        [MonoPInvokeCallback(typeof(EMLoop))]
        public static int em_loop(double _, void* userData)
        {
            Loop func = Marshal.GetDelegateForFunctionPointer<Loop>((IntPtr)userData);
            return func() ? 1 : 0;
        }

        public static void MainLoop(Loop func)
        {
            void* userData = Marshal.GetFunctionPointerForDelegate(func).ToPointer();
            emscripten_request_animation_frame_loop(em_loop, userData);
        }
    }
}
