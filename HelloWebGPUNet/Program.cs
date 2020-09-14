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
            Triangle.Device = Dawn.createDevice(window.Handle);

            WebGPUNative.LoadFuncionPointers(Triangle.Device, Dawn.wgpuGetProcAddress);

            Triangle.Queue = WebGPUNative.wgpuDeviceGetDefaultQueue(Triangle.Device);
            Triangle.SwapChain = Dawn.createSwapChain(Triangle.Device);

            Triangle.CreatePipelineAndBuffers();

            window.Show();

            while(true)
            {
                System.Windows.Forms.Application.DoEvents();
                Triangle.redraw();
            }
            ////Application.Run(window);
        }
    }
}
