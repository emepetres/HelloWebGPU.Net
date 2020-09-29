using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using HelloWebGPUNet.Web;
using HelloWebGPUNet.Web.WebGPU;
using WaveEngine.Bindings.WebGPU;

namespace HelloWebGPUNet
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //Emscripten.wgpu_set_dotnet_entry_point(EntryPoint);
            //Emscripten.wgpu_run();

            var device = Emscripten.CreateDevice(IntPtr.Zero);
            Console.WriteLine("----> Device: " + device);
            var queue = WebGPUNative.wgpuDeviceGetDefaultQueue(device);
            Console.WriteLine("----> Queue: " + device);
            var swapChain = Emscripten.CreateSwapChain(device);
            Console.WriteLine("----> SwapChain: " + device);

            Triangle.Device = device;
            Triangle.Queue = queue;
            Triangle.SwapChain = swapChain;

            Triangle.CreatePipelineAndBuffers();
            Console.WriteLine("----> PipelinesAndBuffers!");

            Emscripten.MainLoop(Triangle.redraw);
        }
    }
}
