using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using HelloWebGPUNet.WebGPU;
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
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var window = new Form1();
            var device = Dawn.createDevice(window.Handle);
            var queue = WebGPUNative.wgpuDeviceGetDefaultQueue(device);
            var swapChain = Dawn.createSwapChain(device);

            Triangle.Device = device;
            Triangle.Queue = queue;
            Triangle.SwapChain = swapChain;
            TriangleCPP.initialize(device, queue, swapChain);

            Triangle.CreatePipelineAndBuffers();
            //TriangleCPP.initializePipelineAndBuffers(Triangle.pipeline, Triangle.vertBuf, Triangle.indxBuf, Triangle.uRotBuf, Triangle.bindGroup);
            //TriangleCPP.createPipelineAndBuffers();

            window.Show();

            while(true)
            {
                System.Windows.Forms.Application.DoEvents();
                Triangle.redraw();
                //TriangleCPP.redraw();
            }
        }
    }
}
